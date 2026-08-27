using System.ComponentModel.DataAnnotations;

namespace UPZMG.Implementations.Roles;

public class EditRole
{
    public required Guid RequesterKey { get; set; }
    public required Guid RoleId { get; set; }
    [MaxLength(40)]
    public required string Name { get; set; }
}
