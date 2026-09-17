using System.Net;
using UPZMG.Implementations.Reports;

namespace UPZMG.Web.Services;

/// <summary>What the API answered to a save: its status and, on success, the stored capture.</summary>
public sealed record ReportSaveResponse(HttpStatusCode Status, ReportSaved? Report);

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
