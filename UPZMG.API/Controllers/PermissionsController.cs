using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Permissions;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("permissions")]
[RequirePermission(AppPermissions.ManageUsers)]
public class PermissionsController(PermissionsRepository permissionsRepository) : ControllerBase
{
    [HttpPost("get-all")]
    // Retrieve all permissions
    public async Task<IActionResult> GetAllPermissions([FromBody] GetAllPermissions request)
    {
        var permissions = await permissionsRepository.GetAllPermissionsAsync(request);
        return Ok(permissions);
    }

    [HttpPost("restore-catalogue")]
    // Adds the permissions declared in code that the table is missing, without touching role assignments
    public async Task<IActionResult> RestoreCatalogue([FromBody] GetAllPermissions request) =>
        Ok(await permissionsRepository.RestoreCatalogueAsync());

    [HttpPost("create")]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermission request)
    {
        try
        {
            var permission = await permissionsRepository.CreatePermissionAsync(request);
            return Ok(permission);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditPermission([FromBody] EditPermission request)
    {
        try
        {
            var permission = await permissionsRepository.EditPermissionAsync(request);
            return Ok(permission);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeletePermission([FromBody] DeletePermission request)
    {
        try
        {
            await permissionsRepository.DeletePermissionAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
