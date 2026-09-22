using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Permissions;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsController(PermissionsRepository permissionsRepository) : ControllerBase
{
    [HttpPost("get-all")]
    // Retrieve all permissions
    public async Task<IActionResult> GetAllPermissions([FromBody] GetAllPermissions request)
    {
        var permissions = await permissionsRepository.GetAllPermissionsAsync(request);
        return Ok(permissions);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermission request)
    {
        var permission = await permissionsRepository.CreatePermissionAsync(request);
        return Ok(permission);
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditPermission([FromBody] EditPermission request)
    {
        var permission = await permissionsRepository.EditPermissionAsync(request);
        return Ok(permission);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeletePermission([FromBody] DeletePermission request)
    {
        await permissionsRepository.DeletePermissionAsync(request);
        return Ok();
    }
}
