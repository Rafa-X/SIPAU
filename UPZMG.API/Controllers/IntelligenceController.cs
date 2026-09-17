using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Intelligence;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("intelligence")]
[Authorize(Roles = AreaRoles)]
public class IntelligenceController(
    IntelligenceRepository intelligenceRepository) : ControllerBase
{
    // Institutional series are shown on the dashboard, which every area sees.
    private const string AreaRoles = "Admin,Control Escolar,Servicios Estudiantiles,Recursos Humanos,Direccion Academica,Recursos Materiales y Servicios Generales";

    // Per-student risk is personal data: only the areas that follow up on students.
    private const string RiskRoles = "Admin,Control Escolar,Servicios Estudiantiles,Direccion Academica";

    private const int MaxPeriodsAhead = 6;

    [HttpGet("forecast")]
    // Least-squares projection of enrolment
    public async Task<ActionResult<EnrollmentForecast>> GetForecast([FromQuery] int ahead = 2) =>
        Ok(await intelligenceRepository.GetForecastAsync(Math.Clamp(ahead, 0, MaxPeriodsAhead)));

    [HttpGet("anomalies")]
    // Statistical alerts over the enrolment, pass and withdrawal series
    public async Task<ActionResult<IReadOnlyList<AnomalyAlert>>> GetAnomalies() =>
        Ok(await intelligenceRepository.GetAnomaliesAsync());

    [HttpGet("dropout")]
    [Authorize(Roles = RiskRoles)]
    // Dropout model: quality, learned rules, risk by career and the most urgent students
    public async Task<ActionResult<DropoutModelReport>> GetDropoutReport() =>
        Ok(await intelligenceRepository.GetDropoutReportAsync());

    [HttpGet("dropout/students")]
    [Authorize(Roles = RiskRoles)]
    // Every scored active student, highest risk first
    public async Task<ActionResult<IReadOnlyList<StudentRisk>>> GetStudentRisks() =>
        Ok(await intelligenceRepository.GetStudentRisksAsync());

    [HttpPost("dropout/retrain")]
    [Authorize(Roles = "Admin")]
    // Retrains now instead of waiting for the data to change
    public async Task<ActionResult<DropoutModelReport>> Retrain([FromBody] RetrainDropoutModel request) =>
        Ok(await intelligenceRepository.RetrainAsync());
}
