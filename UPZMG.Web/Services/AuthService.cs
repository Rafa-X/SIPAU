using System.Security.Claims;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface IAuthService
{
    Task<List<string>> GetPermissionsSnapshotAsync(ClaimsPrincipal user);
}

public class AuthService(ApiClient apiClient, ILogger<AuthService> logger) : IAuthService
{
    private readonly ApiClient _apiClient = apiClient;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<List<string>> GetPermissionsSnapshotAsync(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(userIdValue, out var userId);

        try
        {
            var snapshot = await _apiClient.GetPermissionsSnapshotAsync(userId);  //Get live User snapshot from API
            if (snapshot.Permissions.Count > 0 || snapshot.ReportsPermissions.Count > 0)
                return snapshot.Permissions.Select(r => r.Name).Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to resolve live authorization snapshot for user {userId}.");
        }
        
        return [];
    }
}