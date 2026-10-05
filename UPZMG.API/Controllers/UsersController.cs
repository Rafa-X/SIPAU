using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Users;

using UPZMG.API.Security;
using UPZMG.Implementations.Security;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("users")]
[RequirePermission(AppPermissions.ManageUsers)]
public class UsersController(SecurityRepository securityRepository, UsersRepository usersRepository) : ControllerBase
{
    private async Task<bool> ValidatePermissions<T>(T request)
    {
        var requester_key = typeof(T).GetProperty("RequesterKey")?.GetValue(request) as Guid? 
            ?? throw new UnauthorizedAccessException("RequesterKey is missing or invalid.");
        var required = new List<string> { typeof(T).Name, "FullAccess", "ServiceAccess" };
        var authenticate = await securityRepository.CheckPermissions(requester_key, required);
        return authenticate.Success;
    }


    // The account acting always comes from the token, never from the request body.
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    /// <summary>Suspends or restores an account; a suspended user can no longer sign in.</summary>
    [HttpPost("set-active")]
    public async Task<IActionResult> SetActive([FromBody] SetUserActive request)
    {
        try
        {
            var user = await usersRepository.SetUserActiveAsync(request.UserId, request.IsActive, CurrentUserId);
            return Ok(UsersRepository.ToResponse(user));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("get-all")]
    // Every user, suspended ones included, so an administrator can restore them
    public async Task<IActionResult> GetAllUsers([FromBody] GetAllUsers request)
    {
        //if (!await ValidatePermissions(request)) return Unauthorized();
        var users = await usersRepository.GetAllUsersAsync();

        return Ok(users.Select(UsersRepository.ToResponse));
    }

    [HttpPost("get")]
    // Retrieve user information by ID
    public async Task<IActionResult> GetUserById([FromBody] GetUserById request)
    {
        var user = await usersRepository.GetSystemUserByIdAsync(request.UserId);

        return Ok(UsersRepository.ToResponse(user));
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUser request)
    {
        var user = await usersRepository.CreateUserAsync(request);

        return Created($"/api/users/{user.Id}", UsersRepository.ToResponse(user));
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditUser([FromBody] EditUser request)
    {
        var user = await usersRepository.EditUserAsync(request);

        return Ok(UsersRepository.ToResponse(user));
    }
}