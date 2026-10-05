using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using UPZMG.Persistence.Repository;

namespace UPZMG.API.Security;

/// <summary>
/// Requires a named permission instead of a role name. Roles stay the way the university talks
/// about its areas, but what each one may do is data an administrator can change, which is what
/// the permissions screen promises.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public RequirePermissionAttribute(params string[] permissions)
        : base(PolicyPrefix + string.Join('|', permissions))
    {
    }
}

/// <summary>Any one of the listed permissions is enough.</summary>
public sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

/// <summary>
/// Builds a policy on demand for each permission name, so adding an endpoint needs no
/// registration: the attribute carries the requirement.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
            return _fallback.GetPolicyAsync(policyName);

        var permissions = policyName[RequirePermissionAttribute.PolicyPrefix.Length..]
            .Split('|', StringSplitOptions.RemoveEmptyEntries);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

/// <summary>
/// Resolves the permissions of the signed-in user from the database on every request. The token
/// says who they are; the database says what they may do today. That is what makes a suspended
/// account or a withdrawn permission take effect without waiting for the token to expire.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    SecurityRepository securityRepository,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(id, out var userId))
        {
            logger.LogWarning("A request carried a token without a usable user id.");
            return;
        }

        var granted = await securityRepository.GetEffectivePermissionsAsync(userId);
        if (granted.Overlaps(requirement.Permissions))
        {
            context.Succeed(requirement);
        }
    }
}
