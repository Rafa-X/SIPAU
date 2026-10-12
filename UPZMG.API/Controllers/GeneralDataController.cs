using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Institution;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("general-data")]
// The lookups below are the institution's own catalogues — careers, periods, spaces, degrees —
// and every capture screen fills its selects from them. Requiring "indicadores.consultar" for
// them meant a role created only to capture could not load the form it is meant to fill, so
// they ask for nothing beyond being signed in. Reportable data keeps its own permission.
[Authorize]
public class GeneralDataController(
    GeneralDataRepository generalDataRepository,
    InstitutionRepository institutionRepository) : ControllerBase
{
    /// <summary>Institution master data as captured in the official 911.10 format.</summary>
    [HttpGet("institution")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    public async Task<ActionResult<InstitutionProfile>> GetInstitution() =>
        Ok(await institutionRepository.GetProfileAsync());


    [HttpGet("lookups/careers-selectable")]
    public async Task<ActionResult<List<LookupOption>>> GetCareers() =>
            Ok(await generalDataRepository.GetCareersSelectableAsync());

    [HttpGet("lookups/courses/{careerId:guid}")]
    public async Task<ActionResult<List<LookupOption>>> GetCourses(Guid careerId) =>
        Ok(await generalDataRepository.GetCoursesAsync(careerId));

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