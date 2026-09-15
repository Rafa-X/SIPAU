using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Settings;

namespace UPZMG.Web.Services;

public sealed class PeriodsService(ISecurityService securityService) : IPeriodsService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PeriodItem>> GetPeriodsAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<IReadOnlyList<PeriodItem>>(
            "periods/list", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<PeriodItem>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<HttpStatusCode> CreatePeriodAsync(CreatePeriod request)
    {
        var response = await securityService.SendPostRequestAsync<CreatePeriod, PeriodItem>(
            "periods/create", JsonSerializer.Serialize(request), request.RequesterKey);

        return response.StatusCode;
    }

    public async Task<bool> ActivatePeriodAsync(ActivatePeriod request)
    {
        var response = await securityService.SendPostRequestAsync<ActivatePeriod, object>(
            "periods/activate", JsonSerializer.Serialize(request), request.RequesterKey);

        return response.StatusCode == HttpStatusCode.OK;
    }
}
