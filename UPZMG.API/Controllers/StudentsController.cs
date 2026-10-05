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
    private const string ClassGroupRoles = "Admin,Direccion Academica";

    [HttpGet("ages-new")]
    [RequirePermission(AppPermissions.CaptureStudents)]
    // Search + paginate captured students
    public async Task<IActionResult> GetStudentsAgesNew()
    {
        var result = await studentsRepository.GetStudentsAgesNew();
        return Ok(result);
    }

    [HttpGet("ages-all")]
    [RequirePermission(AppPermissions.CaptureStudents)]
    // Search + paginate captured students
    public async Task<IActionResult> GetStudentsAgesAll()
    {
        var result = await studentsRepository.GetStudentsAgesAll();
        return Ok(result);
    }
}