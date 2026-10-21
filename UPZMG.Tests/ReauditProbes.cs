using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Reports;
using Xunit;

// Probes written by the 2026-09-28 audit. The ones that described a defect now assert the
// behaviour that replaced it; the ones that describe a limitation still on the list are left
// as they were, so they go red the day somebody closes them.
public class ReauditProbes
{
    private static AppDBContext Memory() =>
        new(new DbContextOptionsBuilder<AppDBContext>()
            .UseInMemoryDatabase("probe-" + Guid.NewGuid())
            .Options);

    // Why the repositories now assign the key themselves before building the language links:
    // the owner only gets its id when it is added to the context, and a link built before that
    // keeps the empty Guid it copied. Two students with the same language then collided on the
    // unique (SpeakerId, LanguageId) index and the area was told the matrícula was duplicated.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALinkBuiltBeforeItsOwnerIsAddedKeepsAnEmptySpeakerId(bool student)
    {
        using var db = new AppDBContext(new DbContextOptionsBuilder<AppDBContext>()
            .UseSqlServer("Server=localhost;Database=AuditUnused;Integrated Security=true;TrustServerCertificate=true").Options);
        object owner = Activator.CreateInstance(student ? typeof(Student) : typeof(Employee))!;
        var ownerId = owner.GetType().GetProperty("Id")!;
        var link = new IndigenousLanguagesSpeakers { Id = Guid.NewGuid(), LanguageId = Guid.NewGuid(), SpeakerId = (Guid)ownerId.GetValue(owner)! };
        db.IndigenousLanguagesSpeakers.Add(link);
        db.Add(owner);
        Assert.NotEqual(Guid.Empty, (Guid)ownerId.GetValue(owner)!);
        Assert.Equal(Guid.Empty, link.SpeakerId);
    }

    // The limitation this probe used to record (R07): a student could point at a career or an
    // entry period that did not exist. Both are foreign keys now, and like every other key of the
    // model they refuse the delete of a row still in use instead of cascading it.
    [Fact]
    public void StudentCareerAndEntryPeriodAreForeignKeys()
    {
        using var db = new AppDBContext(new DbContextOptionsBuilder<AppDBContext>()
            .UseSqlServer("Server=localhost;Database=AuditUnused;Integrated Security=true;TrustServerCertificate=true").Options);
        var keys = db.Model.FindEntityType(typeof(Student))!.GetForeignKeys().ToList();

        Assert.Contains(keys, f => f.Properties.Single().Name == "CareerId"
            && f.PrincipalEntityType.ClrType == typeof(Careers) && f.DeleteBehavior == DeleteBehavior.NoAction);
        Assert.Contains(keys, f => f.Properties.Single().Name == "PeriodIdStart"
            && f.PrincipalEntityType.ClrType == typeof(Periods) && f.DeleteBehavior == DeleteBehavior.NoAction);
    }

    /// <summary>
    /// A template with captures can no longer be renamed. The rename moved the file while the
    /// captures went on pointing at the report type and version the old file declared, which
    /// left them with no template to read them back with — the board report simply skipped them.
    /// </summary>
    [Fact]
    public async Task TemplateRenameIsRefusedWhenCapturesExist()
    {
        var folder = Path.Combine(Path.GetTempPath(), "upzmg-audit-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            using var db = Memory();
            var repository = new ReportTemplatesRepository(db, new ReportTemplateStore(folder));
            var template = new ReportTemplateDefinition
            {
                ReportType = "Audit", Version = "2024-2025", DisplayName = "Audit",
                Sections = [new() { Key = "s", Title = "Section", Questions = [new() { Key = "q", Text = "Question", Type = "text" }] }]
            };

            var first = await repository.SaveTemplateAsync(new() { RequesterKey = Guid.NewGuid(), Json = JsonSerializer.Serialize(template) });
            Assert.True(first.Saved);

            db.Reports.Add(new Reports
            {
                Id = Guid.NewGuid(),
                ReportType = "Audit",
                TemplateVersion = "2024-2025",
                Status = nameof(ReportStatus.submitted),
                AnswersJson = "{}",
            });
            await db.SaveChangesAsync();

            // Editing the same version in place is refused too: the answers were filled against it.
            var edited = await repository.SaveTemplateAsync(
                new() { RequesterKey = Guid.NewGuid(), ReplacingFile = "Audit_2024-2025", Json = JsonSerializer.Serialize(template) });
            Assert.False(edited.Saved);

            template.Version = "2025-2026";
            var renamed = await repository.SaveTemplateAsync(
                new() { RequesterKey = Guid.NewGuid(), ReplacingFile = "Audit_2024-2025", Json = JsonSerializer.Serialize(template) });

            Assert.False(renamed.Saved);
            Assert.True(File.Exists(Path.Combine(folder, "Audit_2024-2025.json")));
        }
        finally
        {
            foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    /// <summary>
    /// What the database now guarantees: every capture carries a version to write against, each
    /// area may keep its own capture of a format, there is one board narrative per cycle, and
    /// exactly one period can be active.
    /// </summary>
    [Fact]
    public void ReportAndPeriodBusinessKeysAreDeclared()
    {
        using var db = new AppDBContext(new DbContextOptionsBuilder<AppDBContext>()
            .UseSqlServer("Server=localhost;Database=AuditUnused;Integrated Security=true;TrustServerCertificate=true").Options);

        var entity = db.Model.FindEntityType(typeof(Reports))!;
        Assert.True(entity.FindProperty("RowVersion")!.IsConcurrencyToken);

        var captures = Assert.Single(entity.GetIndexes(),
            i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "ReportType", "TemplateVersion" }));
        Assert.False(captures.IsUnique);

        var narrative = Assert.Single(entity.GetIndexes(),
            i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "TemplateVersion" }));
        Assert.Contains("InformeJunta", narrative.GetFilter());

        var period = db.Model.FindEntityType(typeof(Periods))!;
        var active = Assert.Single(period.GetIndexes(), i => i.Properties.Any(p => p.Name == "IsActive"));
        Assert.True(active.IsUnique);
        Assert.Equal("[IsActive] = 1", active.GetFilter());
    }

    /// <summary>
    /// A time range the check cannot read still produces no conflict, which is why the save path
    /// refuses such a schedule outright instead of storing one nobody can verify. A day written
    /// in short form is now recognised: "Lun" and "Lunes" are the same Monday.
    /// </summary>
    [Theory]
    [InlineData("Lunes", "not-a-time", null)]
    [InlineData("Lun", "09:00-10:00", "Lunes")]
    [InlineData("miércoles", "09:00-10:00", null)]
    public void ScheduleConflictReadsDayAliasesAndIgnoresUnreadableTimes(string day, string time, string? expected)
    {
        var method = typeof(ClassGroupsRepository).GetMethod("FindScheduleConflict", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object[] {
            new List<Schedule> { new() { Day = day, Time = time } },
            new List<Schedule> { new() { Day = "Lunes", Time = "08:00-12:00" } }
        });
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RealOverlappingScheduleIsDetected()
    {
        var method = typeof(ClassGroupsRepository).GetMethod("FindScheduleConflict", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object[] {
            new List<Schedule> { new() { Day = "Lunes", Time = "09:00-10:00" } },
            new List<Schedule> { new() { Day = "Lunes", Time = "08:00-12:00" } }
        });
        Assert.Equal("Lunes", result);
    }
}
