using System.Net;
using System.Text.Json;
using UPZMG.Implementations;
using UPZMG.Implementations.Users;
using UPZMG.Implementations.Roles;
using UPZMG.Implementations.Permissions;
using UPZMG.Persistence.Models;

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
        var response = await securityService.SendPostRequestAsync<CreateUser, SystemUser>(
            $"users/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
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

    public async Task<Role> CreateRole(CreateRole request)
    {
        var response = await securityService.SendPostRequestAsync<CreateRole, Role>(
            $"roles/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Role>(response.Result, ResponseOptions) ?? new Role();
        }
        return new Role();
    }

    public async Task<Role> EditRole(EditRole request)
    {
        var response = await securityService.SendPostRequestAsync<EditRole, Role>(
            $"roles/edit", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Role>(response.Result, ResponseOptions) ?? new Role();
        }
        return new Role();
    }

    public async Task<List<Guid>> GetRolePermissions(GetRolePermissions request)
    {
        var response = await securityService.SendPostRequestAsync<GetRolePermissions, List<Guid>>(
            $"roles/get-permissions", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<Guid>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<bool> SetRolePermissions(SetRolePermissions request)
    {
        var response = await securityService.SendPostRequestAsync<SetRolePermissions, object>(
            $"roles/set-permissions", JsonSerializer.Serialize(request), request.RequesterKey);

        return response.StatusCode == HttpStatusCode.OK;
    }
#endregion

#region Permissions
    public async Task<List<Permission>> GetAllPermissions(GetAllPermissions request)
    {
        var response = await securityService.SendPostRequestAsync<GetAllPermissions, List<Permission>>(
            $"permissions/get-all", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<Permission>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<Permission> CreatePermission(CreatePermission request)
    {
        var response = await securityService.SendPostRequestAsync<CreatePermission, Permission>(
            $"permissions/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Permission>(response.Result, ResponseOptions) ?? new Permission();
        }
        return new Permission();
    }

    public async Task<Permission> EditPermission(EditPermission request)
    {
        var response = await securityService.SendPostRequestAsync<EditPermission, Permission>(
            $"permissions/edit", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Permission>(response.Result, ResponseOptions) ?? new Permission();
        }
        return new Permission();
    }
#endregion
}