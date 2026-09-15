using UPZMG.Implementations.Dashboard;

namespace UPZMG.Web.Services;

public interface IDashboardService
{
    // Null when the API answered without a body.
    Task<DashboardSummary?> GetSummaryAsync();
}
