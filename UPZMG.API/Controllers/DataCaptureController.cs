using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.DataCapture;
using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("data-capture")]
[Authorize]
public class DataCaptureController(
    EmployeesRepository employeesRepository,  
    StudentsRepository studentsRepository,
    ClassGroupsRepository classGroupsRepository,
    InfraestructureRepository infraestructureRepository,
    ScholarshipsRepository scholarshipsRepository,
    ExtensionRepository extensionRepository,
    FinanceRepository financeRepository) : ControllerBase
{

    // The author always comes from the token, never from the request body.
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    [HttpPost("students/create")]
    [RequirePermission(AppPermissions.CaptureStudents)]
    // Create a new student
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudent request)
    {
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
    [RequirePermission(AppPermissions.CaptureStudents)]
    // Search + paginate captured students
    public async Task<IActionResult> GetStudentsList([FromQuery] StudentsListRequest request)
    {
        var result = await studentsRepository.GetStudentsListAsync(request);
        return Ok(result);
    }

    [HttpGet("employees/teachers")]
    [RequirePermission(AppPermissions.CaptureStaff)]
    // Teaching staff already captured
    public async Task<IActionResult> GetTeachersList() =>
        Ok(await employeesRepository.GetTeachersListAsync());

    [HttpPost("employees/create")]
    [RequirePermission(AppPermissions.CaptureStaff)]
    // Create a new employee
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployee request)
    {
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
    [RequirePermission(AppPermissions.CaptureClassGroups)]
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
    [RequirePermission(AppPermissions.CaptureClassGroups)]
    // Captured class groups already registered
    public async Task<IActionResult> GetClassGroupsList() =>
        Ok(await classGroupsRepository.GetClassGroupsListAsync());

    // ── Corrections ─────────────────────────────────────────────────────────
    // Capturing without a way to fix a typo leaves the area with a wrong figure for good,
    // so every daily capture can be read back whole and corrected by the same role.

    [HttpGet("students/{id:guid}")]
    [RequirePermission(AppPermissions.CaptureStudents)]
    public async Task<IActionResult> GetStudent(Guid id) =>
        await studentsRepository.GetStudentAsync(id) is { } student ? Ok(student) : NotFound();

    [HttpPost("students/update")]
    [RequirePermission(AppPermissions.CaptureStudents)]
    public async Task<IActionResult> UpdateStudent([FromBody] UpdateStudent request)
    {
        try
        {
            return Ok(await studentsRepository.UpdateStudentAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("employees/{id:guid}")]
    [RequirePermission(AppPermissions.CaptureStaff)]
    public async Task<IActionResult> GetEmployee(Guid id) =>
        await employeesRepository.GetEmployeeAsync(id) is { } employee ? Ok(employee) : NotFound();

    [HttpPost("employees/update")]
    [RequirePermission(AppPermissions.CaptureStaff)]
    public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployee request)
    {
        try
        {
            return Ok(await employeesRepository.UpdateEmployeeAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("class-groups/{id:guid}")]
    [RequirePermission(AppPermissions.CaptureClassGroups)]
    public async Task<IActionResult> GetClassGroup(Guid id) =>
        await classGroupsRepository.GetAsync(id) is { } group ? Ok(group) : NotFound();

    [HttpPost("class-groups/update")]
    [RequirePermission(AppPermissions.CaptureClassGroups)]
    public async Task<IActionResult> UpdateClassGroup([FromBody] UpdateClassGroup request)
    {
        try
        {
            return Ok(await classGroupsRepository.UpdateAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ── Scholarships ────────────────────────────────────────────────────────

    [HttpPost("scholarships/create")]
    [RequirePermission(AppPermissions.CaptureScholarships)]
    public async Task<IActionResult> CreateScholarship([FromBody] CreateScholarship request)
    {
        try
        {
            var scholarship = await scholarshipsRepository.CreateScholarshipAsync(request);
            return Created($"/api/scholarships/{scholarship.Id}", scholarship);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("scholarships/list")]
    [RequirePermission(AppPermissions.CaptureScholarships)]
    public async Task<IActionResult> GetScholarships() =>
        Ok(await scholarshipsRepository.GetScholarshipsAsync());

    [HttpGet("scholarships/programs")]
    [RequirePermission(AppPermissions.CaptureScholarships)]
    public async Task<IActionResult> GetScholarshipPrograms() =>
        Ok(await scholarshipsRepository.GetProgramsAsync());

    // ── Titulation ──────────────────────────────────────────────────────────

    [HttpPost("titulation/create")]
    [RequirePermission(AppPermissions.CaptureTitulation)]
    public async Task<IActionResult> CreateTitulation([FromBody] CreateTitulation request)
    {
        try
        {
            var titulation = await scholarshipsRepository.CreateTitulationAsync(request);
            return Created($"/api/titulation/{titulation.Id}", titulation);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("titulation/list")]
    [RequirePermission(AppPermissions.CaptureTitulation)]
    public async Task<IActionResult> GetTitulations() =>
        Ok(await scholarshipsRepository.GetTitulationsAsync());

    // ── Infrastructure ──────────────────────────────────────────────────────

    [HttpPost("facilities/create")]
    [RequirePermission(AppPermissions.CaptureFacilities)]
    public async Task<IActionResult> CreateFacility([FromBody] CreateFacility request)
    {
        try
        {
            var facility = await infraestructureRepository.CreateAsync(request);
            return Created($"/api/facilities/{facility.Id}", facility);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("facilities/list")]
    [RequirePermission(AppPermissions.ReadFacilities)]
    public async Task<IActionResult> GetFacilitiesList() =>
        Ok(await infraestructureRepository.GetListAsync());

    // ── Outreach ────────────────────────────────────────────────────────────

    [HttpPost("outreach/create")]
    [RequirePermission(AppPermissions.CaptureOutreach)]
    public async Task<IActionResult> CreateOutreachProject([FromBody] CreateOutreachProject request)
    {
        var userId = CurrentUserId;
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var project = await extensionRepository.CreateAsync(request, userId);
            return Created($"/api/outreach/{project.Id}", project);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("outreach/list")]
    [RequirePermission(AppPermissions.CaptureOutreach)]
    public async Task<IActionResult> GetOutreachList() =>
        Ok(await extensionRepository.GetListAsync());

    // ── Finance ─────────────────────────────────────────────────────────────

    [HttpPost("finance/save")]
    [RequirePermission(AppPermissions.CaptureFinance)]
    public async Task<IActionResult> SaveFinance([FromBody] SaveAnnualFinance request)
    {
        var userId = CurrentUserId;
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var summary = await financeRepository.SaveAsync(request, userId);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("finance/list")]
    [RequirePermission(AppPermissions.CaptureFinance)]
    public async Task<IActionResult> GetFinanceList() =>
        Ok(await financeRepository.GetListAsync());
}