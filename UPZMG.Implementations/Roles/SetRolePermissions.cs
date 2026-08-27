namespace UPZMG.Implementations.Roles;

public class SetRolePermissions
{
    public required Guid RequesterKey { get; set; }
    public required Guid RoleId { get; set; }
    public required List<Guid> PermissionIds { get; set; }
}
