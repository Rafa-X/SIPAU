using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Settings;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("periods")]
[Authorize(Roles = "Admin")]
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
        var activated = await periodsRepository.ActivatePeriodAsync(request);
        return activated ? Ok() : NotFound();
    }
}
