using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Security module.
/// Represents a role that can be assigned to system users.
/// </summary>

public class Role
{
    public Guid Id { get; set; }
    [MaxLength(60)]
    public string Name { get; set; } = null!;
}


/// <summary>
/// Security module.
/// Represents a system user role assignment in the system.
/// </summary>

public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } //fk
    public Guid RoleId { get; set; } //fk
}