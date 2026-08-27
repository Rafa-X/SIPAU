namespace UPZMG.Web.Models.Security;

/// <summary>ViewModel for creating or editing a permission.</summary>
public class PermissionFormViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEditing => Id != Guid.Empty;
}
