using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using UPZMG.Api.Services;
using UPZMG.Persistence;
using UPZMG.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace UPZMG.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AppDBContext db, IConfiguration cfg, ISecurityEventLogger securityLogger) : ControllerBase
{
    private readonly AppDBContext _db = db;
    private readonly IConfiguration _cfg = cfg;
    private readonly ISecurityEventLogger _securityLogger = securityLogger;

    public record TokenExchangeRequest(Guid UserId, string SharedSecret);

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

        var user = await _db.GetSystemUserAsync(req.UserId);
        if (user is null || user.Id == Guid.Empty || user.Role is null)
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

        return Ok(new { access_token = new JwtSecurityTokenHandler().WriteToken(token) });
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

        var userPermissions = await _db.GetSystemUserPermissionsAsync(userId);

        return Ok(userPermissions);
    }
}