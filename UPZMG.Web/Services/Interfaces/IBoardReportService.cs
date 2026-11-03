using UPZMG.Implementations.BoardReport;

namespace UPZMG.Web.Services;

public interface IBoardReportService
{
    /// <summary>One entry per cycle that has official formats; null when the listing could not be read.</summary>
    Task<IReadOnlyList<BoardReportListItem>?> GetCyclesAsync();

    /// <summary>The document of a cycle; null when the cycle has no formats or the API failed.</summary>
    Task<BoardReportDocument?> GetDocumentAsync(string cycle);

    Task<BoardReportSaveResult> SaveNarrativeAsync(string cycle, string blockKey, string? text, string? baseText);

    Task<BoardReportSaveResult> SubmitAsync(string cycle);
}
