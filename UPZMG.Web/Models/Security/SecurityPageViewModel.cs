namespace UPZMG.Web.Models.Security;

public class SecurityPageViewModel
{
    public List<RoleListItemViewModel> Roles { get; set; } = [];
    public Guid? SelectedRoleId { get; set; }
    public string SelectedRoleName { get; set; } = string.Empty;
    public List<PermissionToggleViewModel> Permissions { get; set; } = [];
    public string? StatusMessage { get; set; }
}

public class RoleListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
}

public class PermissionToggleViewModel
{
    public string Name { get; set; } = string.Empty;
    public bool IsAssignedToSelectedRole { get; set; }
    public List<string> AssignedRoleNames { get; set; } = [];
}
