using System.Net;
using System.Text.Json;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public sealed class GeneralDataService(ISecurityService securityService): IGeneralDataService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Careers>> GetCareersAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<Careers>>(
            "general-data/lookups/careers-selectable", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<Careers>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetCareersSelectableAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            "general-data/lookups/careers-selectable", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<IReadOnlyList<LookupOption>> GetAsignaturesAsync(Guid careerId)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            $"general-data/lookups/asignatures/{careerId}", requesterKey);
        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LookupOption>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }
    public async Task<IReadOnlyList<PeriodLookupOption>> GetPeriodsAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<PeriodLookupOption>>(
            "general-data/lookups/periods", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<PeriodLookupOption>>(response.Result, ResponseOptions) ?? [];
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

    public async Task<IReadOnlyList<LookupOption>> GetTeachersAsync() =>
        await GetLookupAsync("general-data/lookups/teachers");

    public async Task<IReadOnlyList<LookupOption>> GetFacilitiesAsync() =>
        await GetLookupAsync("general-data/lookups/facilities");

    private async Task<IReadOnlyList<LookupOption>> GetLookupAsync(string path)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(path, requesterKey);
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