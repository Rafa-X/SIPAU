using System.Net;
using System.Text.Json;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Institution;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public sealed class GeneralDataService(ISecurityService securityService): IGeneralDataService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<InstitutionProfile> GetInstitutionAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<InstitutionProfile>(
                "general-data/institution", requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<InstitutionProfile>(response.Result, ResponseOptions)
                    ?? InstitutionProfile.Unreported;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // The screen shows "sin capturar" rather than failing when the API is down.
        }

        return InstitutionProfile.Unreported;
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

    public async Task<IReadOnlyList<LookupOption>> GetCoursesAsync(Guid careerId)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<LookupOption>>(
            $"general-data/lookups/courses/{careerId}", requesterKey);
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

    public async Task<IReadOnlyList<LookupOption>> GetIndigenousLanguagesAsync() =>
        await GetLookupAsync("general-data/lookups/indigenous-languages");

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
}
