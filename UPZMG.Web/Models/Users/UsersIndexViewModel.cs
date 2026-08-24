using UPZMG.Persistence.Models;

namespace UPZMG.Web.Models.Users;

/// <summary>
/// ViewModel for the user management landing page (mock, no real persistence).
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
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
}
