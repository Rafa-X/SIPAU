using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using UPZMG.Implementations.Security;

namespace UPZMG.Web.Services;

/// <summary>
/// Lets a screen ask for a permission instead of a role name, the same way the API endpoints do.
///
/// Without this the two layers disagreed: the API already answered by permission, but the pages
/// were still written as <c>Roles = "Admin"</c>, so granting "administrar usuarios" to a new role
/// opened the endpoints and left the screen unreachable — which made the permissions screen look
/// like it did nothing. The policy name is the permission name itself, so a page can write
/// <c>[Authorize(Policy = AppPermissions.ManageUsers)]</c> and keep it a compile-time constant.
/// Several names joined with "|" mean any one of them is enough.
/// </summary>
public sealed class WebPermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

public sealed class WebPermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var names = policyName.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Anything that is not a permission of the catalogue is somebody else's policy.
        if (names.Length == 0 || names.Any(name => !AppPermissions.All.Any(p => p.Name == name)))
            return _fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new WebPermissionRequirement(names))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>
/// Ends every cached permission snapshot at once. The administration screens call it after a
/// change of role, permissions or account state, so the change applies on the next page instead
/// of when the cached copy happens to expire — the "sin permiso" screen promises exactly that.
/// </summary>
public sealed class PermissionCacheEpoch
{
    private CancellationTokenSource _epoch = new();

    public IChangeToken Token => new CancellationChangeToken(_epoch.Token);

    public void Invalidate()
    {
        var previous = Interlocked.Exchange(ref _epoch, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }
}

/// <summary>
/// Answers the requirement with the permissions the API reports for the signed-in account, which
/// is the same database the endpoints read. Cached for a few seconds: one page asks several times
/// while it renders, and a change still takes effect while the person is looking at the screen.
/// </summary>
public sealed class WebPermissionHandler(
    ApiClient apiClient,
    IMemoryCache cache,
    PermissionCacheEpoch epoch,
    ILogger<WebPermissionHandler> logger) : AuthorizationHandler<WebPermissionRequirement>
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    // A failure is remembered too, but only for seconds. One page evaluates the permission many
    // times — every AuthorizeView and every entry of the quick-navigation catalogue — and with
    // the API unreachable each of those checks was opening its own doomed connection, which took
    // the page from a quick refusal to tens of seconds. Short enough that a recovering API is
    // picked up almost at once.
    private static readonly TimeSpan CacheFailureFor = TimeSpan.FromSeconds(5);

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, WebPermissionRequirement requirement)
    {
        var claim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty) return;

        var granted = await GetPermissionsAsync(userId);
        if (granted.Overlaps(requirement.Permissions)) context.Succeed(requirement);
    }

    private async Task<HashSet<string>> GetPermissionsAsync(Guid userId)
    {
        var key = $"web_permissions:{userId}";
        if (cache.TryGetValue<HashSet<string>>(key, out var cached) && cached is not null) return cached;

        try
        {
            var snapshot = await apiClient.GetPermissionsSnapshotAsync(userId);
            var names = snapshot.Permissions
                .Select(p => p.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.Ordinal);

            cache.Set(key, names, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheFor)
                .AddExpirationToken(epoch.Token));
            return names;
        }
        catch (Exception ex)
        {
            // An API that cannot answer is not an authorisation: the screen stays closed.
            logger.LogWarning("Could not read the permissions of {User}: {Message}", userId, ex.Message);

            var empty = new HashSet<string>(StringComparer.Ordinal);
            cache.Set(key, empty, CacheFailureFor);
            return empty;
        }
    }
}
