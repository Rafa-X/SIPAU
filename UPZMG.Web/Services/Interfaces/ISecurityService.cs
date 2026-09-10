using UPZMG.Implementations;
using UPZMG.Implementations.Permissions;
using UPZMG.Implementations.Roles;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface ISecurityService
{
    Task<string> GetTokenAsync(Guid userId);
    Task<AccountWebLoginResponse> AuthenticateUserAsync(AccountWebLoginRequest request);
    Task<Guid> GetRequesterKeyAsync();
    Task<SecurityService.DataResponse> SendPostRequestAsync<TRequest, TResponse>(string uri, string json, Guid? userId = null);
    Task<SecurityService.DataResponse> SendGetRequestAsync<TResponse>(
        string path,
        Guid? userId = null,
        bool returnNullOnUnauthorized = false,
        CancellationToken cancellationToken = default);

    Task<List<Role>> GetAllRoles(GetAllRoles request);
    Task<Role> CreateRole(CreateRole request);
    Task<Role> EditRole(EditRole request);
    Task<List<Guid>> GetRolePermissions(GetRolePermissions request);
    Task<bool> SetRolePermissions(SetRolePermissions request);

    Task<List<Permission>> GetAllPermissions(GetAllPermissions request);
    Task<Permission> CreatePermission(CreatePermission request);
    Task<Permission> EditPermission(EditPermission request);
}
