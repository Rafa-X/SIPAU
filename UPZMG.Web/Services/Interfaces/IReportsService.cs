using System.Net;
using UPZMG.Implementations.Reports;

namespace UPZMG.Web.Services;

/// <summary>What the API answered to a save: its status and, on success, the stored capture.</summary>
/// <param name="Message">What the API said when it refused, so the screen can repeat it.</param>
public sealed record ReportSaveResponse(HttpStatusCode Status, ReportSaved? Report, string? Message = null);

public interface IReportsService
{
    Task<IReadOnlyList<ReportTemplateItem>> GetTemplatesAsync();
    Task<ReportTemplateDefinition?> GetTemplateAsync(string reportType, string version);
    Task<IReadOnlyList<ReportListItem>> GetMyReportsAsync();
    Task<ReportDetail?> GetReportAsync(Guid reportId);
    Task<ReportSaveResponse> SaveAsync(SaveReport request);

    // ── Template authoring (Admin) ──────────────────────────────────────────
    Task<string?> GetTemplateSourceAsync(string reportType, string version);
    Task<TemplateSaveResult> SaveTemplateAsync(SaveReportTemplate request);
    Task<TemplateSaveResult> DeleteTemplateAsync(DeleteReportTemplate request);
}
