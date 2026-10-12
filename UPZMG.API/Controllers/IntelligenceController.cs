using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Intelligence;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("intelligence")]
[RequirePermission(AppPermissions.ReadIndicators)]
public class IntelligenceController(
    IntelligenceRepository intelligenceRepository) : ControllerBase
{
    // Institutional series are shown on the dashboard, which every area sees.

    // Per-student risk is personal data: only the areas that follow up on students.

    private const int MaxPeriodsAhead = 6;

    [HttpGet("forecast")]
    // Least-squares projection of enrolment
    public async Task<ActionResult<EnrollmentForecast>> GetForecast([FromQuery] int ahead = 2) =>
        Ok(await intelligenceRepository.GetForecastAsync(Math.Clamp(ahead, 0, MaxPeriodsAhead)));

    [HttpGet("history")]
    // Enrolment, pass rate and outcomes of every period that has registrations
    public async Task<ActionResult<IReadOnlyList<PeriodStats>>> GetHistory() =>
        Ok((await intelligenceRepository.GetHistoryAsync()).Periods);

    [HttpGet("anomalies")]
    // Statistical alerts over the enrolment, pass and withdrawal series
    public async Task<ActionResult<IReadOnlyList<AnomalyAlert>>> GetAnomalies() =>
        Ok(await intelligenceRepository.GetAnomaliesAsync());

    [HttpGet("dropout")]
    [RequirePermission(AppPermissions.ReadRisk)]
    // Dropout model: quality, learned rules, risk by career and the most urgent students
    public async Task<ActionResult<DropoutModelReport>> GetDropoutReport() =>
        Ok(await intelligenceRepository.GetDropoutReportAsync());

    [HttpGet("dropout/students")]
    [RequirePermission(AppPermissions.ReadRisk)]
    // Every scored active student, highest risk first, with the run that produced them
    public async Task<ActionResult<StudentRiskList>> GetStudentRisks() =>
        Ok(await intelligenceRepository.GetStudentRisksAsync());

    [HttpPost("dropout/retrain")]
    [RequirePermission(AppPermissions.TrainRisk)]
    // Retrains now instead of waiting for the data to change
    public async Task<ActionResult<DropoutModelReport>> Retrain([FromBody] RetrainDropoutModel request) =>
        Ok(await intelligenceRepository.RetrainAsync());
}
