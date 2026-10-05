using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Reports;
using Xunit;

// Observational audit probes: assert current behavior, not desired behavior.
public class ReauditProbes
{
    [Fact]
    public async Task TemplateRenameDeletesOldFileWithoutConsultingCaptures()
    {
        var folder = Path.Combine(Path.GetTempPath(), "upzmg-audit-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            using var db = new AppDBContext(new DbContextOptionsBuilder<AppDBContext>().Options);
            var repository = new ReportTemplatesRepository(db, new ReportTemplateStore(folder));
            var template = new ReportTemplateDefinition
            {
                ReportType = "Audit", Version = "2024-2025", DisplayName = "Audit",
                Sections = [new() { Key = "s", Title = "Section", Questions = [new() { Key = "q", Text = "Question", Type = "text" }] }]
            };
            var first = await repository.SaveTemplateAsync(new() { RequesterKey = Guid.NewGuid(), Json = JsonSerializer.Serialize(template) });
            Assert.True(first.Saved);
            template.Version = "2025-2026";
            var second = await repository.SaveTemplateAsync(new() { RequesterKey = Guid.NewGuid(), ReplacingFile = "Audit_2024-2025", Json = JsonSerializer.Serialize(template) });
            Assert.True(second.Saved);
            Assert.False(File.Exists(Path.Combine(folder, "Audit_2024-2025.json")));
            // No database provider was configured: any attempt to inspect captures would fail.
        }
        finally
        {
            foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    [Fact]
    public void ReportRowVersionExistsButNarrativeBusinessKeyIsNotUnique()
    {
        using var db = new AppDBContext(new DbContextOptionsBuilder<AppDBContext>()
            .UseSqlServer("Server=localhost;Database=AuditUnused;Integrated Security=true;TrustServerCertificate=true").Options);
        var entity = db.Model.FindEntityType(typeof(Reports))!;
        Assert.True(entity.FindProperty("RowVersion")!.IsConcurrencyToken);
        var index = Assert.Single(entity.GetIndexes(), i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "ReportType", "TemplateVersion" }));
        Assert.False(index.IsUnique);
        var period = db.Model.FindEntityType(typeof(Periods))!;
        Assert.DoesNotContain(period.GetIndexes(), i => i.IsUnique && i.Properties.Any(p => p.Name == "IsActive"));
    }

    [Theory]
    [InlineData("Lunes", "not-a-time")]
    [InlineData("Lun", "09:00-10:00")]
    public void ScheduleCheckSilentlyAcceptsUnparsedTimeOrDifferentDayAlias(string day, string time)
    {
        var method = typeof(ClassGroupsRepository).GetMethod("FindScheduleConflict", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new object[] {
            new List<Schedule> { new() { Day = day, Time = time } },
            new List<Schedule> { new() { Day = "Lunes", Time = "08:00-12:00" } }
        });
        Assert.Null(result);
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
