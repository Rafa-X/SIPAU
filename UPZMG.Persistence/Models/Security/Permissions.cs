using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Security module.
/// Represents the permissions assigned to a system user, including general permissions and report-specific permissions.
/// </summary>

public class Permission
{
    public Guid Id { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>How the administration screen lists it; the name alone is a slug.</summary>
    [MaxLength(80)]
    public string? DisplayName { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}

public class ReportsPermission
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; } //fk
}

public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public class RoleReportPermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid ReportsPermissionId { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSign { get; set; }
}


//Structure to hold permissions only during project runtime, NOT mapped to the database
[NotMapped]
public class UserPermissions
{
    public List<Permission> Permissions { get; set; } = [];
    public List<RoleReportPermission> ReportsPermissions { get; set; } = [];
}