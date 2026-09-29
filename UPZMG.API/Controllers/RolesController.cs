using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Api.Services;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations;
using UPZMG.Implementations.Roles;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("roles")]
[RequirePermission(AppPermissions.ManageUsers)]
public class RolesController(RolesRepository rolesRepository) : ControllerBase
{
    [HttpPost("get-all")]
    // Retrieve all roles
    public async Task<IActionResult> GetAllRoles([FromBody] GetAllRoles request)
    {
        var roles = await rolesRepository.GetAllRolesAsync(request);
        return Ok(roles);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRole request)
    {
        var role = await rolesRepository.CreateRoleAsync(request);
        return Ok(role);
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditRole([FromBody] EditRole request)
    {
        try
        {
            var role = await rolesRepository.EditRoleAsync(request);
            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteRole([FromBody] DeleteRole request)
    {
        try
        {
            await rolesRepository.DeleteRoleAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("get-permissions")]
    // Retrieve the permission ids currently assigned to a role
    public async Task<IActionResult> GetRolePermissions([FromBody] GetRolePermissions request)
    {
        var permissionIds = await rolesRepository.GetRolePermissionIdsAsync(request);
        return Ok(permissionIds);
    }

    [HttpPost("set-permissions")]
    // Replace the full set of permissions assigned to a role
    public async Task<IActionResult> SetRolePermissions([FromBody] SetRolePermissions request)
    {
        try
        {
            await rolesRepository.SetRolePermissionsAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
