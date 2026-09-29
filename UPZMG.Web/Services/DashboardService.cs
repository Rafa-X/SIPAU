using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Dashboard;

namespace UPZMG.Web.Services;

public sealed class DashboardService(ISecurityService securityService) : IDashboardService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<AcademicIndicators> GetIndicatorsAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<AcademicIndicators>(
                "dashboard/indicators", requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<AcademicIndicators>(response.Result, ResponseOptions)
                    ?? AcademicIndicators.None;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // The card degrades to "sin datos" rather than breaking the dashboard.
        }

        return AcademicIndicators.None;
    }

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
