using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Charts;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("students")]
[Authorize]
public class StudentsController(
    StudentsRepository studentsRepository) : ControllerBase
{
    private const string StudentRoles = "Admin,Control Escolar,Servicios Estudiantiles";
    private const string EmployeeRoles = "Admin,Recursos Humanos";
    private const string ClassGroupRoles = "Admin,Direccion Academica";

    [HttpGet("ages-new")]
    [Authorize(Roles = StudentRoles)]
    // Search + paginate captured students
    public async Task<IActionResult> GetStudentsAgesNew()
    {
        var result = await studentsRepository.GetStudentsAgesNew();
        return Ok(result);
    }

    [HttpGet("ages-all")]
    [Authorize(Roles = StudentRoles)]
    // Search + paginate captured students
    public async Task<IActionResult> GetStudentsAgesAll()
    {
        var result = await studentsRepository.GetStudentsAgesAll();
        return Ok(result);
    }
}