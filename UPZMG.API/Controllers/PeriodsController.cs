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
            return BadRequest("El fin del periodo debe ser posterior a su inicio.");

        // The screen asks for both and for a four-digit year; a direct call stored a period with
        // no code, or one whose year read "20X7" in every listing.
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Period))
            return BadRequest("Indica el código y el periodo.");

        if (request.SchoolCycle.Trim() is not { Length: 4 } year || !year.All(char.IsAsciiDigit))
            return BadRequest("El año del periodo debe tener cuatro dígitos, por ejemplo 2026.");

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
