using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using UPZMG.Api.Services;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Persistence.Repository;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UsersRepository usersRepository, 
    SecurityRepository securityRepository, 
    IConfiguration _cfg, 
    ISecurityEventLogger _securityLogger,
    UPZMG.API.Security.LoginThrottle loginThrottle) : ControllerBase
{
    public record TokenExchangeRequest(Guid UserId, string SharedSecret);
    public record WebLoginRequest(string Email, string Password, string SharedSecret);
    public record WebLoginResponse(Guid UserId, string Email, string RoleName);

    [AllowAnonymous]
    [HttpPost("web-login")]
    // Used by the web application to authenticate credentials through the API
    // and avoid direct DB queries from the web layer.
    public async Task<IActionResult> WebLogin([FromBody] WebLoginRequest req)
    {
        var expected = _cfg["InternalAuth:WebSharedSecret"];
        if (string.IsNullOrWhiteSpace(expected) || req.SharedSecret != expected)
        {
            _securityLogger.LogTokenExchangeRejected(Guid.Empty, "Web login shared secret mismatch or missing");
            return Unauthorized();
        }

        var email = req.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
        {
            _securityLogger.LogTokenExchangeRejected(Guid.Empty, "Web login missing credentials");
            return Unauthorized();
        }

        if (loginThrottle.RetryAfter(email) is { } wait)
        {
            _securityLogger.LogTokenExchangeRejected(Guid.Empty, "Web login temporarily blocked");
            Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Demasiados intentos fallidos. Espera unos minutos e inténtalo de nuevo." });
        }

        var user = await usersRepository.GetSystemUserByEmailAsync(email);
        if (user.Id == Guid.Empty || user.IsActive == false || user.Role is null || string.IsNullOrWhiteSpace(user.Password))
        {
            _securityLogger.LogTokenExchangeRejected(Guid.Empty, "Web login user not found or inactive");
            loginThrottle.RegisterFailure(email);
            return Unauthorized();
        }

        var responseEmail = user.Email?.Trim() ?? string.Empty;
        var responseRole = user.Role.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(responseEmail) || string.IsNullOrWhiteSpace(responseRole))
        {
            _securityLogger.LogTokenExchangeRejected(user.Id, "Web login user has invalid email/role data");
            return Unauthorized();
        }

        var hasher = new PasswordHasher<SystemUser>();
        var passwordResult = hasher.VerifyHashedPassword(user, user.Password, req.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            _securityLogger.LogTokenExchangeRejected(user.Id, "Web login invalid password");
            loginThrottle.RegisterFailure(email);
            return Unauthorized();
        }

        loginThrottle.RegisterSuccess(email);
        return Ok(new WebLoginResponse(user.Id, responseEmail, responseRole));
    }

    [HttpPost("token")]
    // Used by the web application to exchange a shared secret for a JWT token.
    // Fast Authentication context 
    public async Task<IActionResult> Token([FromBody] TokenExchangeRequest req)
    {
        var expected = _cfg["InternalAuth:WebSharedSecret"];
        if (string.IsNullOrWhiteSpace(expected) || req.SharedSecret != expected)
        {
            _securityLogger.LogTokenExchangeRejected(req.UserId, "Shared secret mismatch or missing");
            return Unauthorized();
        }

        var user = await usersRepository.GetSystemUserByIdAsync(req.UserId);

        // A suspended account keeps its web cookie until it expires, so without this check it
        // would go on exchanging it for fresh tokens after the suspension.
        if (user is null || user.Id == Guid.Empty || user.Role is null || !user.IsActive)
        {
            _securityLogger.LogTokenExchangeRejected(req.UserId, "User not found or inactive");
            return Unauthorized();
        }

        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.Name),
        };

        var minutes = int.Parse(jwt["AccessTokenMinutes"] ?? "15");
        var expiresUtc = DateTime.UtcNow.AddMinutes(minutes);
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiresUtc,
            signingCredentials: creds
        );

        _securityLogger.LogTokenIssued(user.Id, user.Role.Name, expiresUtc);

        // The lifetime travels with the token: the caller used to assume one, and a shorter
        // configured lifetime left it caching a token that was already dead.
        return Ok(new
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(token),
            expires_in = (int)Math.Round((expiresUtc - DateTime.UtcNow).TotalSeconds),
        });
    }

    [HttpGet("permissions")]
    [Authorize]
    // Returns the user roles and report permissions based on their claims and database records
    // Fresh Authorization truth
    public async Task<IActionResult> GetCurrentAuthorizationSnapshot()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var userPermissions = await securityRepository.GetSystemUserPermissionsAsync(userId);

        return Ok(userPermissions);
    }
}