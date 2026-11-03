using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Intelligence;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("intelligence")]
// Any of the three gets in; each action then asks for its own. An attribute on the action adds to
// this one, so requiring the indicators here left a role with only "riesgo.consultar" refused.
[RequirePermission(AppPermissions.ReadIndicators, AppPermissions.ReadRisk, AppPermissions.TrainRisk)]
public class IntelligenceController(
    IntelligenceRepository intelligenceRepository,
    AlertsRepository alertsRepository,
    IConfiguration configuration) : ControllerBase
{
    // Whether the trajectories in this database are the demonstration seed. The risk screen says
    // so next to the model's figures, so nobody reads them as results about real students.
    private bool SyntheticData => configuration.GetValue<bool>("Intelligence:SyntheticData");

    // Whose alerts they are always comes from the token, never from the request body.
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    // Institutional series are shown on the dashboard, which every area sees.

    // Per-student risk is personal data: only the areas that follow up on students.

    private const int MaxPeriodsAhead = 6;

    [HttpGet("forecast")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Least-squares projection of enrolment
    public async Task<ActionResult<EnrollmentForecast>> GetForecast([FromQuery] int ahead = 2) =>
        Ok(await intelligenceRepository.GetForecastAsync(Math.Clamp(ahead, 0, MaxPeriodsAhead)));

    [HttpGet("history")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Enrolment, pass rate and outcomes of every period that has registrations
    public async Task<ActionResult<IReadOnlyList<PeriodStats>>> GetHistory() =>
        Ok((await intelligenceRepository.GetHistoryAsync()).Periods);

    [HttpGet("anomalies")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Statistical alerts over the enrolment, pass and withdrawal series
    public async Task<ActionResult<IReadOnlyList<AnomalyAlert>>> GetAnomalies() =>
        Ok(await intelligenceRepository.GetAnomaliesAsync());

    [HttpGet("alerts")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // The same alerts as the signed-in user left them: dismissed ones out, seen ones marked
    public async Task<ActionResult<AlertInbox>> GetAlerts() =>
        CurrentUserId == Guid.Empty ? Unauthorized() : Ok(await alertsRepository.GetInboxAsync(CurrentUserId));

    [HttpPost("alerts/seen")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Marks alerts as seen for the signed-in user
    public async Task<ActionResult<AlertInbox>> MarkAlertsSeen([FromBody] MarkAlertsSeen request) =>
        CurrentUserId == Guid.Empty ? Unauthorized() : Ok(await alertsRepository.MarkSeenAsync(CurrentUserId, request.Keys));

    [HttpPost("alerts/dismiss")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Takes alerts out of the signed-in user's list; everybody else keeps them
    public async Task<ActionResult<AlertInbox>> DismissAlerts([FromBody] DismissAlerts request) =>
        CurrentUserId == Guid.Empty ? Unauthorized() : Ok(await alertsRepository.DismissAsync(CurrentUserId, request.Keys));

    [HttpPost("alerts/restore")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Brings back what the signed-in user dismissed
    public async Task<ActionResult<AlertInbox>> RestoreAlerts([FromBody] RestoreAlerts request) =>
        CurrentUserId == Guid.Empty ? Unauthorized() : Ok(await alertsRepository.RestoreAsync(CurrentUserId));

    [HttpGet("dropout")]
    [RequirePermission(AppPermissions.ReadRisk)]
    // Dropout model: quality, learned rules, risk by career and the most urgent students
    public async Task<ActionResult<DropoutModelReport>> GetDropoutReport() =>
        Ok(await intelligenceRepository.GetDropoutReportAsync() with { SyntheticData = SyntheticData });

    [HttpGet("dropout/students")]
    [RequirePermission(AppPermissions.ReadRisk)]
    // Every scored active student, highest risk first, with the run that produced them
    public async Task<ActionResult<StudentRiskList>> GetStudentRisks() =>
        Ok(await intelligenceRepository.GetStudentRisksAsync());

    [HttpPost("dropout/retrain")]
    [RequirePermission(AppPermissions.TrainRisk)]
    // Retrains now instead of waiting for the data to change
    public async Task<ActionResult<DropoutModelReport>> Retrain([FromBody] RetrainDropoutModel request) =>
        Ok(await intelligenceRepository.RetrainAsync() with { SyntheticData = SyntheticData });
}
