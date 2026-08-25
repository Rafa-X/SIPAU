using UPZMG.Web.Models.Users;

namespace UPZMG.Web.Services;

/// <summary>
/// User catalog for the admin module. In-memory mock CRUD (enforcing
/// "one user = one role"); swap for real DB/API queries later.
/// Registered as a singleton so edits persist across the app session.
/// </summary>
/// <summary>Outcome of a save attempt: the row, or why it was rejected.</summary>
public sealed record UserSaveResult(bool Success, UserListItemModel? User, string? Error);

public interface IUsersService
{
    IReadOnlyList<UserListItemModel> GetUsers();
    IReadOnlyList<string> GetRoles();

    /// <summary>Creates or updates a user, rejecting duplicates.</summary>
    UserSaveResult Save(UserFormViewModel model);
}

public sealed class UsersService : IUsersService
{
    private static readonly string[] RoleCatalog =
    {
        "Admin", "Rectoría", "Planeación", "Control Escolar", "Recursos Humanos", "Finanzas",
    };

    private readonly List<UserListItemModel> _users = new()
    {
        new() { Id = 1, FullName = "Francisco Subdirección", Email = "francisco.planeacion@upzmg.edu.mx", RoleName = "Admin", IsActive = true, LastLogin = DateTime.Now.AddHours(-3) },
        new() { Id = 2, FullName = "Karla Asistente", Email = "karla.asistente@upzmg.edu.mx", RoleName = "Planeación", IsActive = true, LastLogin = DateTime.Now.AddDays(-1) },
        new() { Id = 3, FullName = "Ana Martínez", Email = "ana.martinez@upzmg.edu.mx", RoleName = "Control Escolar", IsActive = true, LastLogin = DateTime.Now.AddDays(-2) },
        new() { Id = 4, FullName = "Carlos López", Email = "carlos.lopez@upzmg.edu.mx", RoleName = "Recursos Humanos", IsActive = false, LastLogin = DateTime.Now.AddDays(-30) },
    };

    public IReadOnlyList<UserListItemModel> GetUsers() => _users;

    public IReadOnlyList<string> GetRoles() => RoleCatalog;

    public UserSaveResult Save(UserFormViewModel model)
    {
        var email = (model.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return new UserSaveResult(false, null, "El correo electrónico es obligatorio.");

        // The email IS the login identity (it is unique on SystemUser), so two
        // rows sharing one would make the account unresolvable at sign-in.
        var clash = _users.FirstOrDefault(u =>
            u.Id != model.Id && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        if (clash is not null)
            return new UserSaveResult(false, null, $"Ya existe un usuario con el correo '{email}' ({clash.FullName}).");

        if (model.IsEditing)
        {
            var existing = _users.FirstOrDefault(u => u.Id == model.Id);
            if (existing is not null)
            {
                existing.FullName = model.FullName;
                existing.Email = email;
                existing.RoleName = model.RoleId;
                existing.IsActive = model.IsActive;
                return new UserSaveResult(true, existing, null);
            }
        }

        var row = new UserListItemModel
        {
            Id = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1,
            FullName = model.FullName,
            Email = email,
            RoleName = model.RoleId,
            IsActive = model.IsActive,
            LastLogin = null,
        };
        _users.Add(row);
        return new UserSaveResult(true, row, null);
    }
}
