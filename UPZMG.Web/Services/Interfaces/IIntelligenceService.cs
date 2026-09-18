using UPZMG.Implementations.Intelligence;

namespace UPZMG.Web.Services;

public interface IIntelligenceService
{
    // None when there is too little history or the API could not answer.
    Task<EnrollmentForecast> GetForecastAsync(int periodsAhead = 2);

    // Empty when nothing stands out or the API could not answer; never throws.
    Task<IReadOnlyList<AnomalyAlert>> GetAnomaliesAsync();

    // Enrolment, pass rate and outcomes per period; empty when there is no history.
    Task<IReadOnlyList<PeriodStats>> GetHistoryAsync();

    // Null when the API refused (role) or failed.
    Task<DropoutModelReport?> GetDropoutReportAsync();

    Task<IReadOnlyList<StudentRisk>> GetStudentRisksAsync();

    // Admin only; null when refused or failed.
    Task<DropoutModelReport?> RetrainAsync();
}
