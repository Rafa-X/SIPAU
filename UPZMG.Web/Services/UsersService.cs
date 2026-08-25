using UPZMG.Implementations;
using UPZMG.Implementations.Users;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models.Users;

namespace UPZMG.Web.Services;

/// <summary>Outcome of a save attempt: the stored user, or why it was rejected.</summary>
public sealed record UserSaveResult(bool Success, SystemUser? User, string? Error);

/// <summary>
/// User catalog for the admin module, backed by the real API (users/roles
/// endpoints) through <see cref="DataService"/>.
///
/// The page depends on this interface rather than on DataService directly, so
/// transport concerns (tokens, DTOs, status codes) never leak into the UI.
/// Every call degrades to an empty result if the API is unreachable — the admin
/// screen must not break when the backend is down.
/// </summary>
public interface IUsersService
{
    Task<IReadOnlyList<UserListModel>> GetUsersAsync(Guid requesterKey);
    Task<IReadOnlyList<Role>> GetRolesAsync(Guid requesterKey);
    Task<UserSaveResult> SaveAsync(Guid requesterKey, UserFormViewModel model);
}

public sealed class UsersService(DataService data, ILogger<UsersService> logger) : IUsersService
{
    public async Task<IReadOnlyList<UserListModel>> GetUsersAsync(Guid requesterKey)
    {
        if (requesterKey == Guid.Empty) return [];

        try
        {
            var users = await data.GetAllUsers(new GetAllUsers { RequesterKey = requesterKey });

            return users.Select(u => new UserListModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                RoleName = u.Role?.Name ?? "—",
                IsActive = u.IsActive,
                LastLogin = u.LastLogin,
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo obtener el catálogo de usuarios desde la API.");
            return [];
        }
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(Guid requesterKey)
    {
        if (requesterKey == Guid.Empty) return [];

        try
        {
            return await data.GetAllRoles(new GetAllRoles { RequesterKey = requesterKey });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo obtener el catálogo de roles desde la API.");
            return [];
        }
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
                var edited = await data.EditUser(new EditUser
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

            var created = await data.CreateUser(new CreateUser
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
