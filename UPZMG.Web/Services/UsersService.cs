using System.Net;
using System.Text.Json;
using UPZMG.Implementations.Users;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models.Users;

namespace UPZMG.Web.Services;

/// <summary>Outcome of a save attempt: the stored user, or why it was rejected.</summary>
public sealed record UserSaveResult(bool Success, SystemUser? User, string? Error);

public sealed class UsersService(
    ISecurityService securityService,
    PermissionCacheEpoch permissionCache,
    ILogger<UsersService> logger)
{
    // API serializes responses using camelCase; match it here so property names bind correctly.
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<SystemUser>> GetAllUsers(GetAllUsers request)
    {
        var response = await securityService.SendPostRequestAsync<GetAllUsers, List<UserView>>(
            $"users/get-all", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            var users = JsonSerializer.Deserialize<List<UserView>>(response.Result, ResponseOptions) ?? [];
            return [.. users.Select(ToUser)];
        }
        return [];
    }

    public async Task<SystemUser> GetUserById(GetUserById request)
    {
        var response = await securityService.SendPostRequestAsync<GetUserById, UserView>(
            $"users/get", JsonSerializer.Serialize(request), request.RequesterKey);

        return Read(response) ?? new SystemUser();
    }

    public async Task<UserSaveResult> CreateUser(CreateUser request)
    {
        var response = await securityService.SendPostRequestAsync<CreateUser, UserView>(
            $"users/create", JsonSerializer.Serialize(request), request.RequesterKey);

        return Outcome(response, "No fue posible crear el usuario.");
    }

    public async Task<UserSaveResult> EditUser(EditUser request)
    {
        var response = await securityService.SendPostRequestAsync<EditUser, UserView>(
            $"users/edit", JsonSerializer.Serialize(request), request.RequesterKey);

        return Outcome(response, "No fue posible actualizar el usuario.");
    }

    /// <summary>Suspends or restores an account; the message explains a refusal.</summary>
    public async Task<UserSaveResult> SetActiveAsync(Guid requesterKey, Guid userId, bool isActive)
    {
        var request = new SetUserActive { RequesterKey = requesterKey, UserId = userId, IsActive = isActive };
        var response = await securityService.SendPostRequestAsync<SetUserActive, UserView>(
            "users/set-active", JsonSerializer.Serialize(request), requesterKey);

        return Outcome(response, "No fue posible cambiar el estatus del usuario.");
    }

    /// <summary>Creates or corrects an account, and says why when the API refuses.</summary>
    public async Task<UserSaveResult> SaveAsync(Guid requesterKey, UserFormViewModel model)
    {
        var email = (model.Email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email))
            return new UserSaveResult(false, null, "El correo electrónico es obligatorio.");

        if (string.IsNullOrWhiteSpace(model.RoleId) || !Guid.TryParse(model.RoleId, out _))
            return new UserSaveResult(false, null, "Selecciona un rol válido.");

        // Sign-in verifies a stored hash, so an account created without a
        // password is an account nobody can ever use. On edit a blank value
        // means "keep the current one".
        if (!model.IsEditing && string.IsNullOrWhiteSpace(model.Password))
            return new UserSaveResult(false, null, "La contraseña es obligatoria para crear un usuario.");

        if (model.IsEditing)
        {
            return await EditUser(new EditUser
            {
                RequesterKey = requesterKey,
                UserId = model.Id,
                UserName = model.UserName,
                Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password,
                IsActive = model.IsActive,
                RoleId = model.RoleId,
            });
        }

        return await CreateUser(new CreateUser
        {
            RequesterKey = requesterKey,
            UserName = model.UserName,
            Email = email,
            RoleId = model.RoleId,
            IsActive = model.IsActive,
            Password = model.Password,
        });
    }

    /// <summary>
    /// A refusal used to come back as an empty user, which every caller took for a stored one:
    /// the screen announced "actualizado correctamente" over a change the API had rejected.
    /// </summary>
    private UserSaveResult Outcome(SecurityService.DataResponse response, string fallback)
    {
        if (response.Sucess && Read(response) is { } user && user.Id != Guid.Empty)
        {
            // A new role or a suspension applies on the person's next page.
            permissionCache.Invalidate();
            return new UserSaveResult(true, user, null);
        }

        logger.LogWarning("A user change was refused with {Status}", response.StatusCode);

        // The API explains its own refusals; anything else is a transport failure, and its raw
        // text ("Request failed with status code…") is no use to the person reading it.
        var explained = response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest;
        return new UserSaveResult(false, null,
            explained && !string.IsNullOrWhiteSpace(response.ErrorMessage) ? response.ErrorMessage : fallback);
    }

    private static SystemUser? Read(SecurityService.DataResponse response)
    {
        if (!response.Sucess || string.IsNullOrWhiteSpace(response.Result)) return null;

        var view = JsonSerializer.Deserialize<UserView>(response.Result, ResponseOptions);
        return view is null ? null : ToUser(view);
    }

    // The API answers with the flat view (role id and name beside the user); the screens work
    // with the user and its role. Reading the view straight into the entity left the role
    // empty, which is why the list showed no role and the edit dialog opened without one.
    private static SystemUser ToUser(UserView view) => new()
    {
        Id = view.Id,
        EmployeeId = view.EmployeeId,
        UserName = view.UserName,
        Email = view.Email,
        IsActive = view.IsActive,
        LastLogin = view.LastLogin,
        Role = view.RoleId is { } roleId
            ? new Role { Id = roleId, Name = view.RoleName ?? string.Empty }
            : null,
    };
}
