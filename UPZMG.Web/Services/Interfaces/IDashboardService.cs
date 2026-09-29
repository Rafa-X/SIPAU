using UPZMG.Implementations.Dashboard;

namespace UPZMG.Web.Services;

public interface IDashboardService
{
    // Null when the API answered without a body.
    Task<DashboardSummary?> GetSummaryAsync();

    /// <summary>The three SII indicators; None when there is nothing graded yet.</summary>
    Task<AcademicIndicators> GetIndicatorsAsync();
}
