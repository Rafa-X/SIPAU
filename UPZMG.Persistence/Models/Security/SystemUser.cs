using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Security module.
/// Represents a system user in the security system.
/// </summary>

public class SystemUser
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } //fk
    [MaxLength(100)]
    public string UserName { get; set; } = null!;
    [MaxLength(100)]
    public string Email { get; set; } = null!;
    [MaxLength(256)]
    public string Password { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }

    [NotMapped] // Not mapped to database, but usable during runtime.
    public Role? Role { get; set; }
}

public class UserResponse
{
    public SystemUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public List<string> Permissions { get; set; } = [];
    public List<string> ReportsPermissions { get; set; } = [];
    public string StatusMessage { get; set; } = string.Empty;
    public bool Success { get; set; }
}