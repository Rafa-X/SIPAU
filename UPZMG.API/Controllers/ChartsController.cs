using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Charts;
using UPZMG.Implementations.DataCapture;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.API.Controllers;

[ApiController]
[Route("charts")]
[Authorize]
public class ChartsController(
    ScholarshipsRepository scholarshipsRepository,
    ChartsRepository chartsRepository) : ControllerBase
{

    // Aggregate figures with no personal data: every area may read them.

    /// <summary>Grants of the active period by source; aggregate only.</summary>
    [HttpGet("scholarships-by-source")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    public async Task<ActionResult<ScholarshipsBySource>> GetScholarshipsBySource() =>
        Ok(await scholarshipsRepository.GetScholarshipsBySourceAsync());

    /// <summary>Titled students against graduates, with the modality breakdown.</summary>
    [HttpGet("titulation-summary")]
    // The titulation consult shows it too, to whoever captures titulations.
    [RequirePermission(AppPermissions.ReadIndicators, AppPermissions.CaptureTitulation)]
    public async Task<ActionResult<TitulationSummary>> GetTitulationSummary() =>
        Ok(await scholarshipsRepository.GetTitulationSummaryAsync());

    /// <summary>Active students by programme and sex; aggregate only, so every area may read it.</summary>
    [HttpGet("gender-distribution")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    public async Task<ActionResult<GenderDistribution>> GetGenderDistribution() =>
        Ok(await chartsRepository.GetGenderDistributionAsync());

    [HttpGet("careers-distribution")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    public async Task<IActionResult> GetCareersDistribution()
    {
        var result = await chartsRepository.GetCareersDistributionAsync();
        return Ok(result);
    }

    [HttpGet("teachers-distribution")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    public async Task<IActionResult> GetTeachersDistribution()
    {
        var result = await chartsRepository.GetTeachersDistributionAsync();
        return Ok(result);
    }

    [HttpGet("groups-distribution-career")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    public async Task<IActionResult> GetGroupsDistributionByCareer()
    {
        var result = await chartsRepository.GetGroupsDistributionByCareerAsync();
        return Ok(result);
    }
}