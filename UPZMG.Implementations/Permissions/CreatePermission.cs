using System.ComponentModel.DataAnnotations;

namespace UPZMG.Implementations.Permissions;

public class CreatePermission
{
    public required Guid RequesterKey { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }
}
