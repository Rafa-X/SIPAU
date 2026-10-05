using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Charts;
using UPZMG.Implementations.DataCapture;

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

    public async Task<ScholarshipsBySource?> GetScholarshipsBySourceAsync() =>
        await GetOrDefaultAsync<ScholarshipsBySource>("charts/scholarships-by-source");

    public async Task<TitulationSummary?> GetTitulationSummaryAsync() =>
        await GetOrDefaultAsync<TitulationSummary>("charts/titulation-summary");

    // Null when the API refused or did not answer; the card then shows its empty state.
    private async Task<T?> GetOrDefaultAsync<T>(string path) where T : class
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<T>(path, requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<T>(response.Result, ResponseOptions);
            }
        }
        catch (HttpRequestException)
        {
        }
        return null;
    }

    public async Task<GenderDistribution?> GetGenderDistributionAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<GenderDistribution>(
            "charts/gender-distribution", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<GenderDistribution>(response.Result, ResponseOptions);
        }
        return null;
    }

    public async Task<TeachersDistribution?> GetTeachersDistributionAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<TeachersDistribution>(
            "charts/teachers-distribution", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<TeachersDistribution>(response.Result, ResponseOptions);
        }
        return null;
    }

    public async Task<List<GroupDistributionCareer>> GetGroupsDistributionByCareerAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<GroupDistributionCareer>>(
            "charts/groups-distribution-career", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<GroupDistributionCareer>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }
}