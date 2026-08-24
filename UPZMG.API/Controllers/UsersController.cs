using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Persistence.Repository;
using UPZMG.Implementations.Users;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("users")]
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


    [HttpPost("get-all")]
    [Authorize]
    // Retrieve all active users
    public async Task<IActionResult> GetAllUsers([FromBody] GetAllUsers request)
    {
        //if (!await ValidatePermissions(request)) return Unauthorized();
        var users = await usersRepository.GetAllUsersAsync();

        return Ok(users);
    }

    [HttpPost("get")]
    [Authorize]
    // Retrieve user information by ID
    public async Task<IActionResult> GetUserById([FromBody] GetUserById request)
    {
        var userPermissions = await usersRepository.GetSystemUserByIdAsync(request.UserId);

        return Ok(userPermissions);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateUser request)
    {
        var user = await usersRepository.CreateUserAsync(request);

        return Created($"/api/users/{user.Id}", user);
    }

    [HttpPost("edit")]
    [Authorize]
    public async Task<IActionResult> EditUser([FromBody] EditUser request)
    {
        var user = await usersRepository.EditUserAsync(request);

        return Ok(user);
    }
}