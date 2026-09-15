using System.Net;
using UPZMG.Implementations.Settings;

namespace UPZMG.Web.Services;

public interface IPeriodsService
{
    Task<IReadOnlyList<PeriodItem>> GetPeriodsAsync();

    // Returns the API status so callers can tell a duplicate code (409) from a failure.
    Task<HttpStatusCode> CreatePeriodAsync(CreatePeriod request);

    Task<bool> ActivatePeriodAsync(ActivatePeriod request);
}
