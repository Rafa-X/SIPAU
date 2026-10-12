using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Intelligence;

namespace UPZMG.Web.Services;

/// <summary>
/// The intelligent layer lives in the API; this service only asks for its results.
///
/// A failure comes back as null rather than as an empty result. The layout calls this on every
/// page and must not break, but a screen that cannot tell "nothing stands out" from "nobody
/// answered" ends up stating the first when the second is true.
/// </summary>
public sealed class IntelligenceService(
    ISecurityService securityService,
    ILogger<IntelligenceService> logger) : IIntelligenceService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public Task<EnrollmentForecast?> GetForecastAsync(int periodsAhead = 2) =>
        GetOrDefaultAsync<EnrollmentForecast>($"intelligence/forecast?ahead={periodsAhead}");

    public async Task<IReadOnlyList<AnomalyAlert>?> GetAnomaliesAsync() =>
        await GetOrDefaultAsync<List<AnomalyAlert>>("intelligence/anomalies");

    public async Task<IReadOnlyList<PeriodStats>?> GetHistoryAsync() =>
        await GetOrDefaultAsync<List<PeriodStats>>("intelligence/history");

    public Task<DropoutModelReport?> GetDropoutReportAsync() =>
        GetOrDefaultAsync<DropoutModelReport>("intelligence/dropout");

    public Task<StudentRiskList?> GetStudentRisksAsync() =>
        GetOrDefaultAsync<StudentRiskList>("intelligence/dropout/students");

    public async Task<DropoutModelReport?> RetrainAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        var request = new RetrainDropoutModel { RequesterKey = requesterKey };

        var response = await securityService.SendPostRequestAsync<RetrainDropoutModel, DropoutModelReport>(
            "intelligence/dropout/retrain", JsonSerializer.Serialize(request), requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<DropoutModelReport>(response.Result, ResponseOptions);
        }

        logger.LogWarning("Retraining the dropout model failed with {Status}", response.StatusCode);
        return null;
    }

    // SendGetRequestAsync throws on any error status or when the API is down.
    private async Task<T?> GetOrDefaultAsync<T>(string path) where T : class
    {
        try
        {
            var requesterKey = await securityService.GetRequesterKeyAsync();
            var response = await securityService.SendGetRequestAsync<T>(path, requesterKey);

            return response.StatusCode == HttpStatusCode.OK && response.Result is not null
                ? JsonSerializer.Deserialize<T>(response.Result, ResponseOptions)
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning("Intelligence request {Path} failed: {Message}", path, ex.Message);
            return null;
        }
    }
}
