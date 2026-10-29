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

    // The career and entry period every sample student belongs to. A student whose career or
    // period does not exist is refused, so the contexts that register students carry both.
    private static readonly Guid Career = Guid.NewGuid();
    private static readonly Guid EntryPeriod = Guid.NewGuid();

    private static AppDBContext Catalogued()
    {
        var db = Memory();
        db.Careers.Add(new Careers { Id = Career, Name = "Ingeniería en Sistemas Computacionales", Alias = "INCO" });
        db.Periods.Add(new Periods
        {
            Id = EntryPeriod, IdCode = "2026-EA", SchoolCycle = "2026", SEP_Period = "Ene-Abr",
            StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 4, 30),
        });
        db.SaveChanges();
        return db;
    }

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
            CareerId = Career,
            PeriodIdStart = EntryPeriod,
            InscriptionType = InscriptionType.NuevoIngreso,
            Status = StudentStatus.Activo,
            IndigenousLanguages = [.. languages],
        };

    [Fact]
    public async Task StudentsWithoutCurpAreAllAcceptedLikeTheIndexAllows()
    {
        using var db = Catalogued();
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
        using var db = Catalogued();
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
    public async Task AStudentOfACareerOrPeriodThatDoesNotExistIsRefused()
    {
        using var db = Catalogued();
        var repository = new StudentsRepository(db);

        var nowhere = Student("UPH0001", string.Empty);
        nowhere.CareerId = Guid.NewGuid();
        var career = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateStudentAsync(nowhere));
        Assert.Contains("carrera", career.Message);

        var never = Student("UPH0002", string.Empty);
        never.PeriodIdStart = Guid.NewGuid();
        var period = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateStudentAsync(never));
        Assert.Contains("periodo", period.Message);

        Assert.Empty(await db.Students.ToListAsync());
    }

    [Fact]
    public async Task ALanguageOutsideTheCatalogueIsRefusedInsteadOfDropped()
    {
        using var db = Catalogued();
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
        using var db = Catalogued();
        var language = new IndigenousLanguages { Id = Guid.NewGuid(), Language = "Nahuatl" };
        db.IndigenousLanguages.Add(language);
        await db.SaveChangesAsync();

        await new StudentsRepository(db).CreateStudentAsync(Student("UPH0001", string.Empty, "Náhuatl"));

        Assert.Equal(language.Id, Assert.Single(await db.IndigenousLanguagesSpeakers.ToListAsync()).LanguageId);
    }

    [Fact]
    public async Task ADuplicatedCurpIsStillRefused()
    {
        using var db = Catalogued();
        var repository = new StudentsRepository(db);

        await repository.CreateStudentAsync(Student("UPH0001", "LORA040301HJCXXX01"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.CreateStudentAsync(Student("UPH0002", "LORA040301HJCXXX01")));
    }

    [Fact]
    public async Task ANewStudentKeepsTheirIndigenousLanguages()
    {
        using var db = Catalogued();
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
        using var db = Catalogued();
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

    // ── What the API refuses although the forms would never send it ─────────────────────────

    [Fact]
    public async Task AStudentTooLongForItsRecordIsRefusedWithTheReason()
    {
        using var db = Catalogued();
        var student = Student("A0099", "");
        student.Name = new string('N', 120);

        // The database refused it as "string or binary data would be truncated", a server error.
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new StudentsRepository(db).CreateStudentAsync(student));

        Assert.Contains("Nombre: máximo 100 caracteres", refusal.Message);
        Assert.Empty(db.Students);
    }

    /// <summary>
    /// A role that accounts still hold cannot be deleted. Deleting it took the role away from
    /// them, and an account without a role cannot sign in: one click locked a whole area out.
    /// </summary>
    [Fact]
    public async Task ARoleThatAccountsHoldIsNotDeleted()
    {
        using var db = Memory();
        var (_, _, plainRoleId) = await WithOneAdministratorAsync(db);
        var clerk = new SystemUser
        {
            Id = Guid.NewGuid(), Email = "ce@upzmg.local", UserName = "ce", Password = "hash", IsActive = true,
        };
        db.SystemUsers.Add(clerk);
        db.UserRoles.Add(new UserRole { UserId = clerk.Id, RoleId = plainRoleId });
        await db.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new RolesRepository(db).DeleteRoleAsync(new() { RequesterKey = Guid.NewGuid(), RoleId = plainRoleId }));

        Assert.Contains("asígnale otro", refusal.Message);
        Assert.True(await db.Roles.AnyAsync(r => r.Id == plainRoleId));
        Assert.True(await db.UserRoles.AnyAsync(ur => ur.UserId == clerk.Id && ur.RoleId == plainRoleId));
    }

    [Fact]
    public async Task APermissionThatDoesNotExistIsNotGranted()
    {
        using var db = Memory();
        var (_, adminRoleId, plainRoleId) = await WithOneAdministratorAsync(db);
        var manage = await db.RolePermissions.Where(rp => rp.RoleId == adminRoleId).Select(rp => rp.PermissionId).FirstAsync();
        var roles = new RolesRepository(db);

        // It ended as a foreign-key violation, a server error.
        await Assert.ThrowsAsync<InvalidOperationException>(() => roles.SetRolePermissionsAsync(
            new() { RequesterKey = Guid.NewGuid(), RoleId = plainRoleId, PermissionIds = [manage, Guid.NewGuid()] }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => roles.SetRolePermissionsAsync(
            new() { RequesterKey = Guid.NewGuid(), RoleId = Guid.NewGuid(), PermissionIds = [manage] }));

        Assert.False(await db.RolePermissions.AnyAsync(rp => rp.RoleId == plainRoleId));
    }

    private static async Task<(AppDBContext Db, CreateClassGroup Group)> GroupContextAsync()
    {
        var db = Catalogued();
        var otherCareer = new Careers { Id = Guid.NewGuid(), Name = "Ingeniería Industrial", Alias = "INDU" };
        var course = new Courses { Id = Guid.NewGuid(), CareerId = Career, CourseName = "Bases de datos" };
        var foreignCourse = new Courses { Id = Guid.NewGuid(), CareerId = otherCareer.Id, CourseName = "Procesos" };
        var teacher = new Employee
        {
            Id = Guid.NewGuid(), EmployeeNumber = 1, FullName = "Laura Mendoza", Email = "laura@upzmg.local",
            Rfc = "MERL850202AB1", Curp = "MERL850202MJCNZR01", Gender = Gender.M, IsTeacher = true,
        };
        var clerk = new Employee
        {
            Id = Guid.NewGuid(), EmployeeNumber = 2, FullName = "Pedro Ruiz", Email = "pedro@upzmg.local",
            Rfc = "RUPE850202AB1", Curp = "RUPE850202HJCZDR01", Gender = Gender.H, IsTeacher = false,
        };
        var room = new SchoolFacility { Id = Guid.NewGuid(), Name = "Aula 1", Problems = [] };

        db.Careers.Add(otherCareer);
        db.Courses.AddRange(course, foreignCourse);
        db.Employees.AddRange(teacher, clerk);
        db.SchoolFacilities.Add(room);
        await db.SaveChangesAsync();

        return (db, new CreateClassGroup
        {
            RequesterKey = Guid.NewGuid(),
            Schedule = [new Schedule { Day = "Lunes", Time = "08:00 - 10:00" }],
            CourseId = course.Id,
            PeriodId = EntryPeriod,
            CareerId = Career,
            TeacherId = teacher.Id,
            FacilityId = room.Id,
            IsTheory = true,
            Capacity = 30,
            Weekhours = 4,
        });
    }

    /// <summary>
    /// What a group points at must exist and agree: a missing room or period ended as a
    /// foreign-key violation, and a course of another career was stored under the wrong one.
    /// </summary>
    [Fact]
    public async Task AGroupPointsAtThingsThatExistAndAgree()
    {
        var (db, valid) = await GroupContextAsync();
        using var _ = db;
        var groups = new ClassGroupsRepository(db);
        var foreignCourse = await db.Courses.Where(c => c.CareerId != Career).Select(c => c.Id).FirstAsync();
        var clerk = await db.Employees.Where(e => !e.IsTeacher).Select(e => e.Id).FirstAsync();

        async Task<string> Refusal(Action<CreateClassGroup> change)
        {
            var request = new CreateClassGroup
            {
                RequesterKey = valid.RequesterKey, Schedule = valid.Schedule, CourseId = valid.CourseId,
                PeriodId = valid.PeriodId, CareerId = valid.CareerId, TeacherId = valid.TeacherId,
                FacilityId = valid.FacilityId, IsTheory = valid.IsTheory, Capacity = valid.Capacity, Weekhours = valid.Weekhours,
            };
            change(request);
            return (await Assert.ThrowsAsync<InvalidOperationException>(() => groups.CreateAsync(request))).Message;
        }

        Assert.Equal("El espacio seleccionado no existe.", await Refusal(g => g.FacilityId = Guid.NewGuid()));
        Assert.Equal("El periodo seleccionado no existe.", await Refusal(g => g.PeriodId = Guid.NewGuid()));
        Assert.Equal("La materia seleccionada no existe.", await Refusal(g => g.CourseId = Guid.NewGuid()));
        Assert.Equal("La materia seleccionada no pertenece a esa carrera.", await Refusal(g => g.CourseId = foreignCourse));
        Assert.Equal("El empleado seleccionado no está registrado como docente.", await Refusal(g => g.TeacherId = clerk));
        Assert.Empty(db.ClassGroups);

        await groups.CreateAsync(valid);
        Assert.Single(db.ClassGroups);
    }

    [Theory]
    // Stored, and as an interval it overlapped nothing: the room looked free at every hour.
    [InlineData("Lunes", "12:00 - 08:00", "termina antes de empezar")]
    [InlineData("Lunes", "08:00 - 08:00", "termina antes de empezar")]
    // Read as Monday alone, so Tuesday to Friday were never checked for a clash.
    [InlineData("Lunes a viernes", "08:00 - 10:00", "No se entiende el día")]
    [InlineData("LunesLunes", "08:00 - 10:00", "No se entiende el día")]
    public async Task AScheduleTheClashCheckCannotTrustIsRefused(string day, string time, string reason)
    {
        var (db, group) = await GroupContextAsync();
        using var _ = db;
        group.Schedule = [new Schedule { Day = day, Time = time }];

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => new ClassGroupsRepository(db).CreateAsync(group));

        Assert.Contains(reason, refusal.Message);
    }

    [Theory]
    [InlineData("Lun")]
    [InlineData("Mié.")]
    [InlineData("lunes y miércoles")]
    public async Task ShortAndCombinedDaysStillRead(string day)
    {
        var (db, group) = await GroupContextAsync();
        using var _ = db;
        group.Schedule = [new Schedule { Day = day, Time = "08:00 - 10:00" }];

        await new ClassGroupsRepository(db).CreateAsync(group);

        Assert.Single(db.ClassGroups);
    }
}
