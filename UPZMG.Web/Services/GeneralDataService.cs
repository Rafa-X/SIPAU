using System.Net;
using System.Text.Json;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public sealed class GeneralDataService(ISecurityService securityService): IGeneralDataService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<LookupOption>> GetCareersAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/careers", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }
    public async Task<IReadOnlyList<LookupOption>> GetPeriodsAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/periods", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetMaxDegreesAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/max-degrees", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetContractTypesAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/contract-types", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetInternalAreasAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/internal-areas", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetSpaceUseTypesAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/space-use-types", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetSectorsAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/sectors", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetCompanySizesAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/company-sizes", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetActivityTypesAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/activity-types", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }
}