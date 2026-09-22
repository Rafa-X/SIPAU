namespace UPZMG.Implementations.Permissions;

public class DeletePermission
{
    public required Guid RequesterKey { get; set; }
    public required Guid PermissionId { get; set; }
}