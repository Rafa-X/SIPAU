using System.Net;
using System.Text.Json;
using UPZMG.Implementations;
using UPZMG.Implementations.Users;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models.Security;

namespace UPZMG.Web.Services;

public sealed class DataService(SecurityService securityService)
{
    // API serializes responses using camelCase; match it here so property names bind correctly.
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

#region Account
    public async Task<AccountWebLoginResponse> AuthenticateUserAsync(AccountWebLoginRequest request)
    {
        var response = await securityService.SendPostRequestAsync<AccountWebLoginRequest, AccountWebLoginResponse>(
            $"auth/web-login", JsonSerializer.Serialize(request));

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<AccountWebLoginResponse>(response.Result, ResponseOptions) ?? new AccountWebLoginResponse();
        }
        return new AccountWebLoginResponse();
    }
#endregion

#region Users
    public async Task<List<SystemUser>> GetAllUsers(GetAllUsers request)
    {
        var response = await securityService.SendPostRequestAsync<GetAllUsers, List<SystemUser>>(
            $"users/get-all", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<SystemUser>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<SystemUser> GetUserById(GetUserById request)
    {
        var response = await securityService.SendPostRequestAsync<GetUserById, SystemUser>(
            $"users/get", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<SystemUser>(response.Result, ResponseOptions) ?? new SystemUser();
        }
        return new SystemUser();
    }

    public async Task<SystemUser> CreateUser(CreateUser request)
    {
        var response = await securityService.SendPostRequestAsync<CreateUser, List<SystemUser>>(
            $"users/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<SystemUser>(response.Result, ResponseOptions) ?? new SystemUser();
        }
        return new SystemUser();
    }

    public async Task<SystemUser> EditUser(EditUser request)
    {
        var response = await securityService.SendPostRequestAsync<EditUser, SystemUser>(
            $"users/edit", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<SystemUser>(response.Result, ResponseOptions) ?? new SystemUser();
        }
        return new SystemUser();
    }
#endregion

#region Roles
    public async Task<List<Role>> GetAllRoles(GetAllRoles request)
    {
        var response = await securityService.SendPostRequestAsync<GetAllRoles, List<Role>>(
            $"roles/get-all", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<Role>>(response.Result, ResponseOptions) ?? new List<Role>();
        }
        return [];
    }

    public async Task<SecurityService.SecurityData> LoadSecurityAsync(Guid requesterKey, Guid? selectedRoleId = null)
    {
        var roles = await GetAllRoles(new GetAllRoles { RequesterKey = requesterKey });

        var roleItems = roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                PermissionCount = 0
            })
            .ToList();

        var selected = selectedRoleId is Guid roleId
            ? roleItems.FirstOrDefault(r => r.Id == roleId)
            : null;

        return new SecurityService.SecurityData(
            roleItems,
            selected?.Id,
            selected?.Name ?? string.Empty,
            [],
            true);
    }
#endregion
}