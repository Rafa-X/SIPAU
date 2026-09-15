using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Dashboard;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("dashboard")]
[Authorize(Roles = "Admin,Teacher")]
public class DashboardController(
    DashboardRepository dashboardRepository) : ControllerBase
{
    [HttpGet("summary")]
    // Home dashboard figures, counted from captured data
    public async Task<ActionResult<DashboardSummary>> GetSummary() =>
        Ok(await dashboardRepository.GetSummaryAsync());
}
