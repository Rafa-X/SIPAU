using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Settings;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("periods")]
[RequirePermission(AppPermissions.ManagePeriods)]
public class PeriodsController(
    PeriodsRepository periodsRepository) : ControllerBase
{
    [HttpGet("list")]
    // All academic periods, oldest first
    public async Task<ActionResult<List<PeriodItem>>> GetPeriods() =>
        Ok(await periodsRepository.GetPeriodsAsync());

    [HttpPost("create")]
    // Create an inactive period; the code must be unique
    public async Task<IActionResult> CreatePeriod([FromBody] CreatePeriod request)
    {
        if (request.EndDate <= request.StartDate)
            return BadRequest("EndDate must be after StartDate.");

        try
        {
            var period = await periodsRepository.CreatePeriodAsync(request);
            return Created($"/periods/{period.Id}", period);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("activate")]
    // Make one period active and every other one inactive
    public async Task<IActionResult> ActivatePeriod([FromBody] ActivatePeriod request)
    {
        try
        {
            var activated = await periodsRepository.ActivatePeriodAsync(request);
            return activated ? Ok() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            // Two administrators switching the active period at the same time.
            return Conflict(new { message = ex.Message });
        }
    }
}
