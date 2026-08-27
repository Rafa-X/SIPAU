using System.ComponentModel.DataAnnotations;

namespace UPZMG.Implementations.Permissions;

public class EditPermission
{
    public required Guid RequesterKey { get; set; }
    public required Guid PermissionId { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }
}
