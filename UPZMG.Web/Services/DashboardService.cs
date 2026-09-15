using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Dashboard;

namespace UPZMG.Web.Services;

public sealed class DashboardService(ISecurityService securityService) : IDashboardService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<DashboardSummary?> GetSummaryAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<DashboardSummary>(
            "dashboard/summary", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<DashboardSummary>(response.Result, ResponseOptions);
        }
        return null;
    }
}
