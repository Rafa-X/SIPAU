using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.DataCapture;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("general-data")]
[Authorize(Roles = "Admin,Teacher")]
public class GeneralDataController(
    GeneralDataRepository generalDataRepository) : ControllerBase
{

[HttpGet("lookups/careers")]
    public async Task<ActionResult<List<LookupOption>>> GetCareers() =>
        Ok(await generalDataRepository.GetCareersAsync());

    [HttpGet("lookups/periods")]
    public async Task<ActionResult<List<LookupOption>>> GetPeriods() =>
        Ok(await generalDataRepository.GetPeriodsAsync());

}