using System.Security.Claims;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface IAuthService
{
    Task<List<string>> GetPermissionsSnapshotAsync(ClaimsPrincipal user);
}

public class AuthService : IAuthService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApiClient apiClient, ILogger<AuthService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<string>> GetPermissionsSnapshotAsync(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(userIdValue, out var userId);

        try
        {
            var snapshot = await _apiClient.GetPermissionsSnapshotAsync(userId);  //Get live User snapshot from API
            if (snapshot.Roles.Count > 0 || snapshot.ReportsPermissions.Count > 0)
                return snapshot.Roles.Select(r => r.Name).Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to resolve live authorization snapshot for user {userId}.");
        }
        
        return new List<string>();
    }
}