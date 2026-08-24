using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

/// <summary>A saved report row surfaced in the "recent reports" list.</summary>
public sealed record RecentReport(Guid Id, string ReportType, string DisplayName, string Version, ReportStatus Status, DateTime LastModified);

/// <summary>A loaded draft (its template key + saved answers) for resuming.</summary>
public sealed record ReportDraft(Guid Id, string ReportType, string Version, ReportStatus Status, Dictionary<string, string?> Answers);

/// <summary>
/// Reports 911 module (compañero's V1 logic, ported to Blazor). Templates come
/// from versioned JSON files; drafts persist to the DB. Every DB call is wrapped
/// so a missing 'reports' table degrades gracefully instead of throwing.
/// </summary>
public interface IReportsService
{
    Task<IReadOnlyList<ReportTemplateDefinition>> GetTemplatesAsync();
    Task<ReportTemplateDefinition?> GetTemplateAsync(string reportType, string version);
    Task<IReadOnlyList<RecentReport>> GetRecentReportsAsync(Guid userId);
    Task<ReportDraft?> GetReportAsync(Guid id, Guid userId);

    /// <summary>
    /// Persists a report. Pass <paramref name="reportId"/> to update the row a
    /// draft was loaded from — omit it only for a brand new capture. Returns the
    /// stored id, or null when the 'reports' table is not available.
    /// </summary>
    Task<Guid?> SaveAsync(Guid userId, Guid? reportId, string reportType, string version,
                          ReportStatus status, Dictionary<string, string?> answers);
}

public sealed class ReportsService : IReportsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IWebHostEnvironment _env;
    private readonly IServiceScopeFactory _scopeFactory;

    public ReportsService(IWebHostEnvironment env, IServiceScopeFactory scopeFactory)
    {
        _env = env;
        _scopeFactory = scopeFactory;
    }

    private string TemplatesDir => Path.Combine(_env.ContentRootPath, "Views", "Reports", "Templates");

    // Cached once per process. Avoids spamming EF with failing queries (which log
    // at 'fail' level) when the 'reports' table hasn't been migrated yet.
    // OBJECT_ID returns NULL for a missing table instead of throwing.
    private static bool? _reportsTableExists;
    private static async Task<bool> ReportsTableExistsAsync(AppDBContext db)
    {
        if (_reportsTableExists is { } cached) return cached;
        try
        {
            // Materialise first: taking First() server-side makes EF log an
            // "unordered FirstOrDefault" advisory for what is a scalar query.
            var rows = await db.Database
                .SqlQueryRaw<int?>("SELECT OBJECT_ID('reports') AS Value")
                .ToListAsync();
            _reportsTableExists = rows.FirstOrDefault() is not null;
        }
        catch
        {
            _reportsTableExists = false; // DB unreachable
        }
        return _reportsTableExists.Value;
    }

    public async Task<IReadOnlyList<ReportTemplateDefinition>> GetTemplatesAsync()
    {
        if (!Directory.Exists(TemplatesDir)) return [];

        var catalog = new List<ReportTemplateDefinition>();
        foreach (var file in Directory.GetFiles(TemplatesDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var json = await File.ReadAllTextAsync(file);
            var template = JsonSerializer.Deserialize<ReportTemplateDefinition>(json, JsonOptions);
            if (template is not null) catalog.Add(template);
        }
        return catalog.OrderBy(t => t.ReportType).ThenBy(t => t.Version).ToList();
    }

    public async Task<ReportTemplateDefinition?> GetTemplateAsync(string reportType, string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        var path = Path.Combine(TemplatesDir, $"{reportType}_{version}.json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<ReportTemplateDefinition>(await File.ReadAllTextAsync(path), JsonOptions);
    }

    public async Task<IReadOnlyList<RecentReport>> GetRecentReportsAsync(Guid userId)
    {
        if (userId == Guid.Empty) return [];
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
            if (!await ReportsTableExistsAsync(db)) return [];

            var rows = await db.Reports.AsNoTracking()
                .Where(r => r.CreatedBy == userId)
                .OrderByDescending(r => r.LastModified)
                .Take(5)
                .Select(r => new { r.Id, r.ReportType, r.TemplateVersion, r.Status, r.LastModified })
                .ToListAsync();

            var templates = await GetTemplatesAsync();

            return rows.Select(r => new RecentReport(
                r.Id, r.ReportType,
                templates.FirstOrDefault(t => t.ReportType.Equals(r.ReportType, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? r.ReportType,
                r.TemplateVersion ?? string.Empty,
                Enum.TryParse<ReportStatus>(r.Status, out var s) ? s : ReportStatus.draft,
                r.LastModified)).ToList();
        }
        catch (Exception)
        {
            return []; // table missing / DB down → empty history
        }
    }

    public async Task<ReportDraft?> GetReportAsync(Guid id, Guid userId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
            if (!await ReportsTableExistsAsync(db)) return null;

            var report = await db.Reports.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.CreatedBy == userId);
            if (report is null) return null;

            return new ReportDraft(report.Id, report.ReportType, report.TemplateVersion ?? string.Empty,
                Enum.TryParse<ReportStatus>(report.Status, out var s) ? s : ReportStatus.draft,
                ParseAnswers(report.AnswersJson));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Guid?> SaveAsync(Guid userId, Guid? reportId, string reportType, string version,
                                       ReportStatus status, Dictionary<string, string?> answers)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
            if (!await ReportsTableExistsAsync(db)) return null;

            var json = JsonSerializer.Serialize(answers, JsonOptions);

            // Saving a draft twice must update the same report, not pile up a
            // new row on every click — the history would otherwise fill with
            // half-finished copies of the same 911 capture.
            if (reportId is { } id)
            {
                var existing = await db.Reports.FirstOrDefaultAsync(r => r.Id == id && r.CreatedBy == userId);
                if (existing is not null)
                {
                    existing.Status = status.ToString();
                    existing.AnswersJson = json;
                    existing.TemplateVersion = version;
                    existing.LastModified = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return existing.Id;
                }
            }

            var created = new Reports
            {
                Id = Guid.NewGuid(),
                CreatedBy = userId,
                ReportType = reportType,
                TemplateVersion = version,
                Status = status.ToString(),
                AnswersJson = json,
                DateCreated = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };
            db.Reports.Add(created);
            await db.SaveChangesAsync();
            return created.Id;
        }
        catch (Exception)
        {
            return null; // table missing → caller shows a hint but the UX still flows
        }
    }

    private static Dictionary<string, string?> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.OrdinalIgnoreCase);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions);
        return parsed is null ? new(StringComparer.OrdinalIgnoreCase) : new(parsed, StringComparer.OrdinalIgnoreCase);
    }
}
