using UPZMG.Persistence.Models;

namespace UPZMG.Web.Models.Users;

/// <summary>
/// State of the user management listing.
/// Mirrors seguridad.usuarios_sistema joined with its single role.
/// </summary>
public class UsersIndexViewModel
{
    public List<SystemUser> Users { get; set; } = new();
}

/// <summary>
/// Represents a single user row in the user management listing.
/// </summary>
public class UserListModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
}
