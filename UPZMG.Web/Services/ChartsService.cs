using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Charts;

namespace UPZMG.Web.Services;

public sealed class ChartsService(ISecurityService securityService): IChartsService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<ChartCategory>> GetCareersDistributionAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<ChartCategory>>(
            "charts/careers-distribution", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<ChartCategory>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }
}