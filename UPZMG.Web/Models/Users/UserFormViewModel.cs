using Microsoft.AspNetCore.Mvc.Rendering;

namespace UPZMG.Web.Models.Users;

/// <summary>
/// ViewModel for creating or editing a system user (mock, no real persistence).
/// Enforces the "one user = one role" model: a single RoleId is selected.
/// </summary>
public class UserFormViewModel
{
    public int? Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary> Single role assignment (one user has exactly one role). </summary>
    public string RoleId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> Roles { get; set; } = new();

    public bool IsEditing => Id.HasValue;
}
