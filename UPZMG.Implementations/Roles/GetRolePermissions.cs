namespace UPZMG.Implementations.Roles;

public class GetRolePermissions
{
    public required Guid RequesterKey { get; set; }
    public required Guid RoleId { get; set; }
}
