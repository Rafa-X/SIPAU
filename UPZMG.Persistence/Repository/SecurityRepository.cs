using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Answers what a signed-in person is allowed to do. The answer comes from the database on every
/// request rather than from the token, so suspending an account or changing a role's permissions
/// takes effect immediately instead of when the token happens to expire.
/// </summary>
public class SecurityRepository(AppDBContext db)
{
    /// <summary>
    /// The permission names of an active user. An inactive account has none, which is what makes
    /// a suspension immediate even while its token is still cryptographically valid.
    /// </summary>
    public async Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId)
    {
        if (userId == Guid.Empty) return [];

        var isActive = await db.SystemUsers.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.IsActive)
            .FirstOrDefaultAsync();

        if (!isActive) return [];

        var names = await (
            from ur in db.UserRoles.AsNoTracking()
            join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where ur.UserId == userId
            select p.Name)
            .Distinct()
            .ToListAsync();

        return [.. names];
    }

    /// <summary>
    /// Everything a user may do, for the permissions snapshot. The two collections are queried
    /// separately on purpose: joining them dropped every ordinary permission of a role that had
    /// no report permissions, which is most of them.
    /// </summary>
    public async Task<UserPermissions> GetSystemUserPermissionsAsync(Guid userId)
    {
        if (userId == Guid.Empty) return new UserPermissions();

        var roleIds = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0) return new UserPermissions();

        var permissions = await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where roleIds.Contains(rp.RoleId)
            select new Permission { Id = p.Id, Name = p.Name, DisplayName = p.DisplayName, Description = p.Description })
            .Distinct()
            .ToListAsync();

        var reportsPermissions = await db.RoleReportsPermissions.AsNoTracking()
            .Where(rrp => roleIds.Contains(rrp.RoleId))
            .Select(rrp => new RoleReportPermission
            {
                RoleId = rrp.RoleId,
                ReportsPermissionId = rrp.ReportsPermissionId,
                CanEdit = rrp.CanEdit,
                CanSign = rrp.CanSign,
            })
            .ToListAsync();

        return new UserPermissions
        {
            Permissions = permissions,
            ReportsPermissions = reportsPermissions,
        };
    }
}
