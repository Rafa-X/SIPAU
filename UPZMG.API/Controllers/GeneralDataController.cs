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

    [HttpGet("lookups/asignatures/{careerId:guid}")]
    public async Task<ActionResult<List<LookupOption>>> GetAsignatures(Guid careerId) =>
        Ok(await generalDataRepository.GetAsignaturesAsync(careerId));

    [HttpGet("lookups/periods")]
    public async Task<ActionResult<List<PeriodLookupOption>>> GetPeriods() =>
        Ok(await generalDataRepository.GetPeriodsAsync());

    [HttpGet("lookups/max-degrees")]
    public async Task<ActionResult<List<LookupOption>>> GetMaxDegrees() =>
        Ok(await generalDataRepository.GetMaxDegreesAsync());

    [HttpGet("lookups/contract-types")]
    public async Task<ActionResult<List<LookupOption>>> GetContractTypes() =>
        Ok(await generalDataRepository.GetContractTypesAsync());

    [HttpGet("lookups/internal-areas")]
    public async Task<ActionResult<List<LookupOption>>> GetInternalAreas() =>
        Ok(await generalDataRepository.GetInternalAreasAsync());

    [HttpGet("lookups/teachers")]
    public async Task<ActionResult<List<LookupOption>>> GetTeachers() =>
        Ok(await generalDataRepository.GetTeachersAsync());

    [HttpGet("lookups/facilities")]
    public async Task<ActionResult<List<LookupOption>>> GetFacilities() =>
        Ok(await generalDataRepository.GetFacilitiesAsync());

}