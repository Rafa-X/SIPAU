using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Reports;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class ReportsRepository(AppDBContext db)
{
    // Every capture of one user, newest first
    public async Task<List<ReportListItem>> GetMineAsync(Guid userId)
    {
        if (userId == Guid.Empty) return [];

        // The board narrative lives in the same table but is not a format: listed here it showed
        // up among the captures as "InformeJunta", with a button that opened it in the wrong editor.
        var rows = await db.Reports
            .AsNoTracking()
            .Where(r => r.CreatedBy == userId && r.ReportType != BoardReportRepository.NarrativeReportType)
            .OrderByDescending(r => r.LastModified)
            .Select(r => new { r.Id, r.ReportType, r.TemplateVersion, r.Status, r.DateCreated, r.LastModified })
            .ToListAsync();

        return rows
            .Select(r => new ReportListItem(r.Id, r.ReportType, r.TemplateVersion ?? string.Empty,
                ParseStatus(r.Status), r.DateCreated, r.LastModified))
            .ToList();
    }

    // One capture with its answers; null when it does not exist or belongs to someone else
    public async Task<ReportDetail?> GetForUserAsync(Guid reportId, Guid userId)
    {
        if (reportId == Guid.Empty || userId == Guid.Empty) return null;

        var report = await db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId && r.CreatedBy == userId
                && r.ReportType != BoardReportRepository.NarrativeReportType);

        return report is null
            ? null
            : new ReportDetail(report.Id, report.ReportType, report.TemplateVersion ?? string.Empty,
                ParseStatus(report.Status), report.DateCreated, report.LastModified, ParseAnswers(report.AnswersJson),
                Version(report.RowVersion));
    }

    /// <summary>
    /// Creates or updates a capture. Only drafts change, and a submission must be
    /// complete and balanced under the same engine the capture screen runs.
    /// </summary>
    public async Task<SaveReportResult> SaveAsync(SaveReport request, Guid userId, ReportTemplateDefinition template)
    {
        if (request.Status == ReportStatus.submitted)
        {
            var form = new ReportFormState(template, request.Answers);
            if (!form.IsComplete)
            {
                var problems = form.Missing
                    .Select(m => $"Falta «{m.QuestionText}» ({m.SectionTitle}).")
                    .Concat(form.Violations.Select(v => $"{v.QuestionText} ({v.SectionTitle}): {v.Message}"))
                    .ToList();

                return new SaveReportResult(SaveReportOutcome.Incomplete, Problems: problems);
            }
        }

        var now = DateTime.UtcNow;
        var answersJson = JsonSerializer.Serialize(request.Answers);
        Reports report;

        if (request.ReportId is { } reportId)
        {
            var existing = await db.Reports.FirstOrDefaultAsync(r =>
                r.Id == reportId && r.CreatedBy == userId &&
                r.ReportType == template.ReportType && r.TemplateVersion == template.Version);

            if (existing is null) return new SaveReportResult(SaveReportOutcome.NotFound);

            // A submitted format was declared as official, so it is never edited in place.
            if (ParseStatus(existing.Status) != ReportStatus.draft) return new SaveReportResult(SaveReportOutcome.Locked);

            // The row version the form was opened with. Reading the row again inside this
            // request only catches a write in the microseconds between read and save; what has
            // to be checked is whether anything changed since the person opened the capture.
            if (!string.IsNullOrEmpty(request.RowVersion)
                && request.RowVersion != Version(existing.RowVersion))
            {
                return new SaveReportResult(SaveReportOutcome.Stale);
            }

            existing.Status = request.Status.ToString();
            existing.AnswersJson = answersJson;
            existing.LastModified = now;
            report = existing;
        }
        else
        {
            report = new Reports
            {
                Id = Guid.NewGuid(),
                CreatedBy = userId,
                ReportType = template.ReportType,
                TemplateVersion = template.Version,
                Status = request.Status.ToString(),
                AnswersJson = answersJson,
                DateCreated = now,
                LastModified = now,
            };
            db.Reports.Add(report);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Somebody else wrote this capture between our read and our write. Whether they
            // submitted it or only saved, ours is no longer an informed change.
            return new SaveReportResult(SaveReportOutcome.Locked);
        }

        return new SaveReportResult(
            request.ReportId is null ? SaveReportOutcome.Created : SaveReportOutcome.Updated,
            new ReportSaved(report.Id, ParseStatus(report.Status), report.LastModified, Version(report.RowVersion)));
    }

    // The database stamps the row version as bytes; it travels as text.
    private static string? Version(byte[]? rowVersion) =>
        rowVersion is null ? null : Convert.ToBase64String(rowVersion);

    private static ReportStatus ParseStatus(string? status) =>
        Enum.TryParse<ReportStatus>(status, ignoreCase: true, out var parsed) ? parsed : ReportStatus.draft;

    // A corrupted payload reads as an empty capture instead of breaking the page.
    private static Dictionary<string, string?> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
            return parsed is null ? new(StringComparer.OrdinalIgnoreCase) : new(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
