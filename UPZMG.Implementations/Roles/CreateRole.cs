using System.ComponentModel.DataAnnotations;

namespace UPZMG.Implementations.Roles;

public class CreateRole
{
    public required Guid RequesterKey { get; set; }
    [MaxLength(40)]
    public required string Name { get; set; }
}
