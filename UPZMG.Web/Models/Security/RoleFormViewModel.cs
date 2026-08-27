namespace UPZMG.Web.Models.Security;

/// <summary>ViewModel for creating or editing a role.</summary>
public class RoleFormViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEditing => Id != Guid.Empty;
}
