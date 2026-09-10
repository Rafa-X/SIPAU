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
    ILogger<UsersService> logger)
{
    // API serializes responses using camelCase; match it here so property names bind correctly.
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

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

        try
        {
            if (model.IsEditing)
            {
                var edited = await EditUser(new EditUser
                {
                    RequesterKey = requesterKey,
                    UserId = model.Id,
                    UserName = model.UserName,
                    Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password,
                    IsActive = model.IsActive,
                    RoleId = model.RoleId,
                });

                return edited.Id == Guid.Empty
                    ? new UserSaveResult(false, null, "La API no pudo actualizar el usuario.")
                    : new UserSaveResult(true, edited, null);
            }

            var created = await CreateUser(new CreateUser
            {
                RequesterKey = requesterKey,
                UserName = model.UserName,
                Email = email,
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                Password = model.Password,
            });

            // The API creates a system user *for an existing employee*, matched
            // by email. Saying so beats surfacing a raw 500.
            return created is null
                ? new UserSaveResult(false, null,
                    $"No se pudo crear el usuario. Verifica que exista un empleado registrado con el correo '{email}'.")
                : new UserSaveResult(true, created, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fallo al guardar el usuario contra la API.");
            return new UserSaveResult(false, null, "No se pudo contactar al servicio de usuarios.");
        }
    }
}
