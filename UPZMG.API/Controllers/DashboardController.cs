using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Dashboard;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("dashboard")]
[RequirePermission(AppPermissions.ReadIndicators)]
public class DashboardController(
    DashboardRepository dashboardRepository) : ControllerBase
{
    /// <summary>Aprovechamiento, deserción and eficiencia terminal with the university's own formulas.</summary>
    [HttpGet("indicators")]
    public async Task<ActionResult<AcademicIndicators>> GetIndicators() =>
        Ok(await dashboardRepository.GetAcademicIndicatorsAsync());

    [HttpGet("summary")]
    // Home dashboard figures, counted from captured data
    public async Task<ActionResult<DashboardSummary>> GetSummary() =>
        Ok(await dashboardRepository.GetSummaryAsync());
}
