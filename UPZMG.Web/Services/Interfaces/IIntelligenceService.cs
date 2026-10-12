using UPZMG.Implementations.Intelligence;

namespace UPZMG.Web.Services;

/// <summary>
/// The intelligent layer as the screens see it. Every method answers null when the request
/// failed, and an empty result only when the institution really has nothing: showing "sin
/// anomalías" because the API did not answer is a different statement from "nothing stands out".
/// </summary>
public interface IIntelligenceService
{
    // Null when the API could not answer; EnrollmentForecast.None when there is too little history.
    Task<EnrollmentForecast?> GetForecastAsync(int periodsAhead = 2);

    // Null when the API could not answer; empty when nothing stands out.
    Task<IReadOnlyList<AnomalyAlert>?> GetAnomaliesAsync();

    // Null when the API could not answer; empty when there is no history yet.
    Task<IReadOnlyList<PeriodStats>?> GetHistoryAsync();

    // Null when the API refused (permission) or failed.
    Task<DropoutModelReport?> GetDropoutReportAsync();

    // Null when the API refused or failed; carries the run that scored the students.
    Task<StudentRiskList?> GetStudentRisksAsync();

    // Needs the retraining permission; null when refused or failed.
    Task<DropoutModelReport?> RetrainAsync();
}
