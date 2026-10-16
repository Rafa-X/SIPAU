using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Security;
using UPZMG.Implementations.Users;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Persistence.Repository;
using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Hardening;

/// <summary>
/// Rules the repositories enforce, exercised against a context rather than against a model.
/// Each one is here because it failed in a way the area could not have guessed: a correct
/// capture refused as duplicated, a language that disappeared, an administrator locked out of
/// the screen they were standing on.
/// </summary>
public class BusinessRuleTests
{
    private static AppDBContext Memory() =>
        new(new DbContextOptionsBuilder<AppDBContext>()
            .UseInMemoryDatabase("rules-" + Guid.NewGuid())
            .Options);

    private static CreateStudent Student(string enrollment, string curp, params string[] languages) =>
        new()
        {
            RequesterKey = Guid.NewGuid(),
            EnrollmentNumber = enrollment,
            CURP = curp,
            Name = "Ana",
            MiddleName = "López",
            LastName = "Ruiz",
            BirthDate = new DateOnly(2004, 3, 1),
            Gender = Gender.M,
            PostalCode = "45640",
            MaritalStatus = MaritalStatus.Soltero,
            BirthCountry = "México",
            BirthState = "Jalisco",
            Address = "Calle 1",
            CareerId = Guid.NewGuid(),
            PeriodIdStart = Guid.NewGuid(),
            InscriptionType = InscriptionType.NuevoIngreso,
            Status = StudentStatus.Activo,
            IndigenousLanguages = [.. languages],
        };

    [Fact]
    public async Task StudentsWithoutCurpAreAllAcceptedLikeTheIndexAllows()
    {
        using var db = Memory();
        var repository = new StudentsRepository(db);

        await repository.CreateStudentAsync(Student("UPH0001", string.Empty));
        await repository.CreateStudentAsync(Student("UPH0002", string.Empty));

        // The unique index is filtered on [CURP] <> '', so the database accepts both. Checking
        // a blank value in the application refused everyone after the first student.
        Assert.Equal(2, await db.Students.CountAsync());
    }

    [Fact]
    public async Task AStudentWithoutCurpCanBeCorrectedWhenOthersHaveNoneEither()
    {
        using var db = Memory();
        var repository = new StudentsRepository(db);
        await repository.CreateStudentAsync(Student("UPH0001", string.Empty));
        var second = await repository.CreateStudentAsync(Student("UPH0002", string.Empty));

        // The correction compared the blank CURP with the other student's blank one and
        // refused with "ya existe otro alumno con la CURP ''".
        var source = Student("UPH0002", string.Empty);
        var corrected = await repository.UpdateStudentAsync(new UpdateStudent
        {
            Id = second.Id, RequesterKey = source.RequesterKey, EnrollmentNumber = source.EnrollmentNumber,
            CURP = source.CURP, Name = source.Name, MiddleName = source.MiddleName, LastName = source.LastName,
            BirthDate = source.BirthDate, Gender = source.Gender, PostalCode = source.PostalCode,
            MaritalStatus = source.MaritalStatus, BirthCountry = source.BirthCountry, BirthState = source.BirthState,
            Address = source.Address, CareerId = source.CareerId, PeriodIdStart = source.PeriodIdStart,
            InscriptionType = source.InscriptionType, Status = StudentStatus.BajaTemporal,
            IndigenousLanguages = [],
        });

        Assert.Equal(StudentStatus.BajaTemporal, corrected.Status);
    }

    [Fact]
    public async Task ALanguageOutsideTheCatalogueIsRefusedInsteadOfDropped()
    {
        using var db = Memory();
        db.IndigenousLanguages.Add(new IndigenousLanguages { Id = Guid.NewGuid(), Language = "Maya Yucateco" });
        await db.SaveChangesAsync();

        // The form used to offer "Maya"; the catalogue says "Maya Yucateco", and the language
        // vanished from the record without anyone being told.
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new StudentsRepository(db).CreateStudentAsync(Student("UPH0001", string.Empty, "Maya")));

        Assert.Contains("Maya Yucateco", refusal.Message);
        Assert.Empty(await db.Students.ToListAsync());
    }

    [Fact]
    public async Task ALanguageMatchesTheCatalogueWithOrWithoutItsAccent()
    {
        using var db = Memory();
        var language = new IndigenousLanguages { Id = Guid.NewGuid(), Language = "Nahuatl" };
        db.IndigenousLanguages.Add(language);
        await db.SaveChangesAsync();

        await new StudentsRepository(db).CreateStudentAsync(Student("UPH0001", string.Empty, "Náhuatl"));

        Assert.Equal(language.Id, Assert.Single(await db.IndigenousLanguagesSpeakers.ToListAsync()).LanguageId);
    }

    [Fact]
    public async Task ADuplicatedCurpIsStillRefused()
    {
        using var db = Memory();
        var repository = new StudentsRepository(db);

        await repository.CreateStudentAsync(Student("UPH0001", "LORA040301HJCXXX01"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.CreateStudentAsync(Student("UPH0002", "LORA040301HJCXXX01")));
    }

    [Fact]
    public async Task ANewStudentKeepsTheirIndigenousLanguages()
    {
        using var db = Memory();
        var language = new IndigenousLanguages { Id = Guid.NewGuid(), Language = "Náhuatl" };
        db.IndigenousLanguages.Add(language);
        await db.SaveChangesAsync();

        var student = await new StudentsRepository(db).CreateStudentAsync(Student("UPH0001", "LORA040301HJCXXX01", "Náhuatl"));

        var link = Assert.Single(await db.IndigenousLanguagesSpeakers.ToListAsync());
        Assert.Equal(student.Id, link.SpeakerId);
        Assert.NotEqual(Guid.Empty, link.SpeakerId);
        Assert.Equal(language.Id, link.LanguageId);
    }

    [Fact]
    public async Task TwoStudentsCanSpeakTheSameLanguage()
    {
        using var db = Memory();
        db.IndigenousLanguages.Add(new IndigenousLanguages { Id = Guid.NewGuid(), Language = "Wixárika" });
        await db.SaveChangesAsync();

        var repository = new StudentsRepository(db);
        await repository.CreateStudentAsync(Student("UPH0001", "AAAA040301HJCXXX01", "Wixárika"));
        await repository.CreateStudentAsync(Student("UPH0002", "BBBB040301HJCXXX02", "Wixárika"));

        // Both links used to be written with an empty SpeakerId, so the second one collided on
        // the unique (SpeakerId, LanguageId) index and the area was told the matrícula existed.
        var links = await db.IndigenousLanguagesSpeakers.ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.Equal(2, links.Select(l => l.SpeakerId).Distinct().Count());
    }

    // ── Administration cannot be given away by accident ─────────────────────

    private static async Task<(Guid UserId, Guid AdminRoleId, Guid PlainRoleId)> WithOneAdministratorAsync(AppDBContext db)
    {
        var permission = new Permission { Id = Guid.NewGuid(), Name = AppPermissions.ManageUsers };
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var plainRole = new Role { Id = Guid.NewGuid(), Name = "Control Escolar" };
        var user = new SystemUser
        {
            Id = Guid.NewGuid(), Email = "admin@upzmg.local", UserName = "admin",
            Password = "hash", IsActive = true,
        };

        db.Permissions.Add(permission);
        db.Roles.AddRange(adminRole, plainRole);
        db.SystemUsers.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();

        return (user.Id, adminRole.Id, plainRole.Id);
    }

    [Fact]
    public async Task TheLastAdministratorCannotBeSuspendedFromTheEditDialog()
    {
        using var db = Memory();
        var (userId, _, _) = await WithOneAdministratorAsync(db);
        var repository = new UsersRepository(db);

        var request = new EditUser { RequesterKey = Guid.NewGuid(), UserId = userId, UserName = "admin", IsActive = false };

        // Reached from the correction form instead of the suspension switch, this used to go
        // through: the guards lived only in SetUserActive.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EditUserAsync(request, requestedBy: Guid.NewGuid()));

        Assert.True(await db.SystemUsers.Where(u => u.Id == userId).Select(u => u.IsActive).FirstAsync());
    }

    [Fact]
    public async Task TheLastAdministratorCannotMoveToARoleThatCannotAdminister()
    {
        using var db = Memory();
        var (userId, _, plainRoleId) = await WithOneAdministratorAsync(db);
        var repository = new UsersRepository(db);

        var request = new EditUser
        {
            RequesterKey = Guid.NewGuid(),
            UserId = userId,
            UserName = "admin",
            IsActive = true,
            RoleId = plainRoleId.ToString(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EditUserAsync(request, requestedBy: Guid.NewGuid()));
    }

    [Fact]
    public async Task AnEmptyRoleLeavesTheAccountWhereItWas()
    {
        using var db = Memory();
        var (userId, adminRoleId, _) = await WithOneAdministratorAsync(db);
        var repository = new UsersRepository(db);

        await repository.EditUserAsync(
            new EditUser { RequesterKey = Guid.NewGuid(), UserId = userId, IsActive = true, UserName = "nuevo" },
            requestedBy: userId);

        // A missing role used to be stored as an empty Guid, which pointed the account at a
        // role that does not exist: no permissions, and no way to grant them back.
        Assert.Equal(adminRoleId, await db.UserRoles.Where(r => r.UserId == userId).Select(r => r.RoleId).FirstAsync());
        Assert.Equal("nuevo", await db.SystemUsers.Where(u => u.Id == userId).Select(u => u.UserName).FirstAsync());
    }

    [Fact]
    public async Task TheOnlyRoleThatAdministersCannotLoseThePermission()
    {
        using var db = Memory();
        var (_, adminRoleId, _) = await WithOneAdministratorAsync(db);
        var repository = new RolesRepository(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SetRolePermissionsAsync(new()
            {
                RequesterKey = Guid.NewGuid(),
                RoleId = adminRoleId,
                PermissionIds = [],
            }));
    }

    [Fact]
    public async Task ARoleWithNoActiveAccountDoesNotCountAsAnAdministrator()
    {
        using var db = Memory();
        var (userId, adminRoleId, _) = await WithOneAdministratorAsync(db);

        // A second role holds the permission, but the only account in it is suspended.
        var spare = new Role { Id = Guid.NewGuid(), Name = "Suplente" };
        var suspended = new SystemUser
        {
            Id = Guid.NewGuid(), Email = "baja@upzmg.local", UserName = "baja",
            Password = "hash", IsActive = false,
        };
        var permissionId = await db.Permissions.Select(p => p.Id).FirstAsync();

        db.Roles.Add(spare);
        db.SystemUsers.Add(suspended);
        db.UserRoles.Add(new UserRole { UserId = suspended.Id, RoleId = spare.Id });
        db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = spare.Id, PermissionId = permissionId });
        await db.SaveChangesAsync();

        var roles = new RolesRepository(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => roles.SetRolePermissionsAsync(new() { RequesterKey = Guid.NewGuid(), RoleId = adminRoleId, PermissionIds = [] }));

        Assert.NotEqual(Guid.Empty, userId);
    }

    // ── The permission catalogue belongs to the code ─────────────────────────

    [Fact]
    public async Task ASystemPermissionCannotBeRenamedOrDeleted()
    {
        using var db = Memory();
        var permission = new Permission { Id = Guid.NewGuid(), Name = AppPermissions.ManageUsers };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync();

        var repository = new PermissionsRepository(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.EditPermissionAsync(
            new() { RequesterKey = Guid.NewGuid(), PermissionId = permission.Id, Name = "otro.nombre" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeletePermissionAsync(
            new() { RequesterKey = Guid.NewGuid(), PermissionId = permission.Id }));

        Assert.Equal(AppPermissions.ManageUsers, await db.Permissions.Select(p => p.Name).FirstAsync());
    }

    [Fact]
    public async Task RestoringTheCatalogueAddsWhatIsMissingAndTouchesNoAssignment()
    {
        using var db = Memory();
        var role = new Role { Id = Guid.NewGuid(), Name = "Control Escolar" };
        var kept = new Permission { Id = Guid.NewGuid(), Name = AppPermissions.CaptureStudents, DisplayName = "viejo" };
        db.Roles.Add(role);
        db.Permissions.Add(kept);
        db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = role.Id, PermissionId = kept.Id });
        await db.SaveChangesAsync();

        var result = await new PermissionsRepository(db).RestoreCatalogueAsync();

        Assert.Equal(AppPermissions.All.Count - 1, result.Added);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Unknown);

        // What an administrator granted survives: this is the safe alternative to re-running
        // the seed, which rebuilds every role assignment from scratch.
        var assignment = Assert.Single(await db.RolePermissions.ToListAsync());
        Assert.Equal(kept.Id, assignment.PermissionId);
        Assert.Equal(role.Id, assignment.RoleId);
    }

    [Fact]
    public async Task APermissionThatNothingChecksIsNotCreated()
    {
        using var db = Memory();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new PermissionsRepository(db)
            .CreatePermissionAsync(new() { RequesterKey = Guid.NewGuid(), Name = "inventado.permiso" }));

        Assert.Empty(await db.Permissions.ToListAsync());
    }

    // ── Figures the university reports ──────────────────────────────────────

    [Fact]
    public async Task AStudentWithTwoScholarshipSourcesIsOneBeneficiary()
    {
        using var db = Memory();
        var period = new Periods
        {
            Id = Guid.NewGuid(), IdCode = "2026-1", SchoolCycle = "2026", SEP_Period = "Ene-Abr",
            StartDate = new DateOnly(2026, 1, 12), EndDate = new DateOnly(2026, 4, 24), IsActive = true,
        };
        var studentId = Guid.NewGuid();
        db.Periods.Add(period);
        db.ScolarshipAssigneds.AddRange(
            new ScolarshipAssigned
            {
                Id = Guid.NewGuid(), StudentId = studentId, PeriodId = period.Id,
                ScolarshipProgramId = Guid.NewGuid(), FinantialSource = FinantialSource.Federal,
                Amount = 1000, AssignedDate = new DateOnly(2026, 1, 20),
            },
            new ScolarshipAssigned
            {
                Id = Guid.NewGuid(), StudentId = studentId, PeriodId = period.Id,
                ScolarshipProgramId = Guid.NewGuid(), FinantialSource = FinantialSource.Estatal,
                Amount = 500, AssignedDate = new DateOnly(2026, 1, 20),
            });
        await db.SaveChangesAsync();

        var result = await new ScholarshipsRepository(db).GetScholarshipsBySourceAsync();

        Assert.Equal(2, result.Grants);
        Assert.Equal(1, result.Beneficiaries);
        Assert.Equal(1500, result.TotalAmount);
        Assert.Equal("2026 Ene-Abr", result.Scope);
    }

    [Fact]
    public async Task ScholarshipsFallBackToHistoryButSaySo()
    {
        using var db = Memory();
        var active = new Periods
        {
            Id = Guid.NewGuid(), IdCode = "2026-2", SchoolCycle = "2026", SEP_Period = "May-Ago",
            StartDate = new DateOnly(2026, 5, 4), EndDate = new DateOnly(2026, 8, 21), IsActive = true,
        };
        var earlier = new Periods
        {
            Id = Guid.NewGuid(), IdCode = "2025-3", SchoolCycle = "2025", SEP_Period = "Sep-Dic",
            StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2025, 12, 12), IsActive = false,
        };
        db.Periods.AddRange(active, earlier);
        db.ScolarshipAssigneds.Add(new ScolarshipAssigned
        {
            Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), PeriodId = earlier.Id,
            ScolarshipProgramId = Guid.NewGuid(), FinantialSource = FinantialSource.Federal,
            Amount = 2000, AssignedDate = new DateOnly(2025, 9, 15),
        });
        await db.SaveChangesAsync();

        var result = await new ScholarshipsRepository(db).GetScholarshipsBySourceAsync();

        // Showing the history is better than an empty chart, but calling it the current period
        // turned a fallback into a claim about this cycle.
        Assert.Equal(1, result.Beneficiaries);
        Assert.Equal("todo el historial", result.Scope);
    }

    [Fact]
    public async Task AGroupWithNobodyEnrolledIsStillCounted()
    {
        using var db = Memory();
        var period = new Periods
        {
            Id = Guid.NewGuid(), IdCode = "2026-1", SchoolCycle = "2026", SEP_Period = "Ene-Abr",
            StartDate = new DateOnly(2026, 1, 12), EndDate = new DateOnly(2026, 4, 24), IsActive = true,
        };
        var career = new Careers { Id = Guid.NewGuid(), Name = "Ingeniería en Software", Alias = "ISW" };

        db.Periods.Add(period);
        db.Careers.Add(career);
        db.ClassGroups.Add(new ClassGroups
        {
            Id = Guid.NewGuid(), PeriodId = period.Id, CareerId = career.Id, CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(), FacilityId = Guid.NewGuid(), Capacity = 30, Weekhours = 4,
            IsTheory = true, Enrolled = 0, Schedule = [new Schedule { Day = "Lunes", Time = "08:00-10:00" }],
        });
        await db.SaveChangesAsync();

        var result = await new ChartsRepository(db).GetGroupsDistributionByCareerAsync();

        // The join to enrolments dropped every group nobody had enrolled in yet, so a programme
        // that had just opened its groups vanished from the chart.
        var item = Assert.Single(result);
        Assert.Equal("ISW", item.Career);
        Assert.Equal(1, item.Groups);
        Assert.Equal(0, item.Students);
    }
}
