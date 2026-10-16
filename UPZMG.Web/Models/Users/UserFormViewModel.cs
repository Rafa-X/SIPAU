namespace UPZMG.Web.Models.Users;

/// <summary>
/// Form state for creating or editing a system user.
/// Enforces the "one user = one role" model: a single RoleId is selected.
/// </summary>
public class UserFormViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsEditing => Id != Guid.Empty;
    public string Password { get; set; } = string.Empty;
}
