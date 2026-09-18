using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;

namespace UPZMG.API.Controllers;

[ApiController]
[Route("charts")]
[Authorize]
public class ChartsController(
    ChartsRepository chartsRepository) : ControllerBase
{
    private const string EmployeeRoles = "Admin,Recursos Humanos";

    // Aggregate figures with no personal data: every area may read them.
    private const string AreaRoles = "Admin,Control Escolar,Servicios Estudiantiles,Recursos Humanos,Direccion Academica,Recursos Materiales y Servicios Generales";

    [HttpGet("careers-distribution")]
    [Authorize(Roles = EmployeeRoles)]
    public async Task<IActionResult> GetCareersDistribution()
    {
        var result = await chartsRepository.GetCareersDistributionAsync();
        return Ok(result);
    }

    [HttpGet("teachers-distribution")]
    [Authorize(Roles = AreaRoles)]
    public async Task<IActionResult> GetTeachersDistribution()
    {
        var result = await chartsRepository.GetTeachersDistributionAsync();
        return Ok(result);
    }

    [HttpGet("groups-distribution-career")]
    [Authorize(Roles = EmployeeRoles)]
    public async Task<IActionResult> GetGroupsDistributionByCareer()
    {
        var result = await chartsRepository.GetGroupsDistributionByCareerAsync();
        return Ok(result);
    }
}