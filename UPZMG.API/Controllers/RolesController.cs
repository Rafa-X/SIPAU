using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Api.Services;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("roles")]
public class RolesController(RolesRepository rolesRepository) : ControllerBase
{
    [HttpPost("get-all")]
    [Authorize]
    // Retrieve all roles
    public async Task<IActionResult> GetAllRoles([FromBody] GetAllRoles request)
    {
        var roles = await rolesRepository.GetAllRolesAsync(request);
        return Ok(roles);
    }
}
