using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Charts;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("students")]
[Authorize]
public class StudentsController(
    StudentsRepository studentsRepository) : ControllerBase
{
    // Both histograms are counts per age, with no name or record attached, and they feed the
    // students chart every area can open. Asking for the capture permission locked three roles
    // out of a page they are allowed to see, and the page failed instead of saying so.
    [HttpGet("ages-new")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Ages of the students registered as new entrants, grouped
    public async Task<IActionResult> GetStudentsAgesNew()
    {
        var result = await studentsRepository.GetStudentsAgesNew();
        return Ok(result);
    }

    [HttpGet("ages-all")]
    [RequirePermission(AppPermissions.ReadIndicators)]
    // Ages of every registered student, grouped
    public async Task<IActionResult> GetStudentsAgesAll()
    {
        var result = await studentsRepository.GetStudentsAgesAll();
        return Ok(result);
    }
}