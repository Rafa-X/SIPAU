using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.DataCapture;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("data-capture")]
[Authorize]
public class DataCaptureController(
    SecurityRepository securityRepository, 
    EmployeesRepository employeesRepository, 
    StudentsRepository studentsRepository,
    ClassGroupsRepository classGroupsRepository) : ControllerBase
{
    private const string StudentRoles = "Admin,Control Escolar,Servicios Estudiantiles";
    private const string EmployeeRoles = "Admin,Recursos Humanos";
    private const string ClassGroupRoles = "Admin,Dirección Académica";

    private async Task<bool> ValidatePermissions<T>(T request)
    {
        var requester_key = typeof(T).GetProperty("RequesterKey")?.GetValue(request) as Guid? 
            ?? throw new UnauthorizedAccessException("RequesterKey is missing or invalid.");
        var required = new List<string> { typeof(T).Name, "FullAccess", "ServiceAccess" };
        var authenticate = await securityRepository.CheckPermissions(requester_key, required);
        return authenticate.Success;
    }


    [HttpPost("students/create")]
    [Authorize(Roles = StudentRoles)]
    // Create a new student
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudent request)
    {
        //if (!await ValidatePermissions(request)) return Unauthorized();
        try
        {
            var student = await studentsRepository.CreateStudentAsync(request);
            return Created($"/api/students/{student.Id}", student);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("students/list")]
    [Authorize(Roles = StudentRoles)]
    // Search + paginate captured students
    public async Task<IActionResult> GetStudentsList([FromQuery] StudentsListRequest request)
    {
        var result = await studentsRepository.GetStudentsListAsync(request);
        return Ok(result);
    }

    [HttpGet("employees/teachers")]
    [Authorize(Roles = EmployeeRoles)]
    // Teaching staff already captured
    public async Task<IActionResult> GetTeachersList() =>
        Ok(await employeesRepository.GetTeachersListAsync());

    [HttpPost("employees/create")]
    [Authorize(Roles = EmployeeRoles)]
    // Create a new employee
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployee request)
    {
        //if (!await ValidatePermissions(request)) return Unauthorized();
        try
        {
            var employee = await employeesRepository.CreateEmployeeAsync(request);
            return Created($"/api/employees/{employee.Id}", employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("class-groups/create")]
    [Authorize(Roles = ClassGroupRoles)]
    public async Task<IActionResult> CreateClassGroup([FromBody] CreateClassGroup request)
    {
        try
        {
            var group = await classGroupsRepository.CreateAsync(request);
            return Created($"/api/class-groups/{group.Id}", group);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("class-groups/list")]
    [Authorize(Roles = ClassGroupRoles)]
    // Captured class groups already registered
    public async Task<IActionResult> GetClassGroupsList() =>
        Ok(await classGroupsRepository.GetClassGroupsListAsync());
}