using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Queries;

public static class SecurityQueries
{
    public static async Task<SystemUser> GetSystemUserAsync(this AppDBContext db, string email)
    {
        return await (
            from su in db.SystemUsers.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on su.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where su.Email == email && su.IsActive
            select new SystemUser
            {
                Id = su.Id,
                EmployeeId = su.EmployeeId,
                Email = su.Email,
                Password = su.Password,
                IsActive = su.IsActive,
                LastLogin = su.LastLogin,
                Role = new Role
                {
                    Id = r.Id,
                    Name = r.Name
                }
            })
            .FirstOrDefaultAsync() ?? new SystemUser();
    }

    public static async Task<SystemUser> GetSystemUserAsync(this AppDBContext db, Guid userId)
    {
        return await (
            from su in db.SystemUsers.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on su.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where su.Id == userId && su.IsActive
            select new SystemUser
            {
                Id = su.Id,
                EmployeeId = su.EmployeeId,
                Email = su.Email,
                Password = su.Password,
                IsActive = su.IsActive,
                LastLogin = su.LastLogin,
                Role = new Role
                {
                    Id = r.Id,
                    Name = r.Name
                }
            })
            .FirstOrDefaultAsync() ?? new SystemUser();
    }

    public static async Task<UserPermissions> GetSystemUserPermissionsAsync(this AppDBContext db, Guid userId)
    {
        if (userId == Guid.Empty)
            return new UserPermissions();
        
        return await (
            from ur  in db.UserRoles.AsNoTracking()
            join r   in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            join rp  in db.RolePermissions.AsNoTracking() on r.Id equals rp.RoleId
            join p   in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            join rrp in db.RoleReportsPermissions.AsNoTracking() on rp.RoleId equals rrp.RoleId
            where ur.UserId == userId && rp.RoleId == r.Id && rrp.RoleId == r.Id
            select new UserPermissions
            {
                Permissions = new List<Permission>
                {
                    new() {
                        Id = p.Id,
                        Name = p.Name
                    }
                },
                ReportsPermissions = new List<RoleReportPermission>
                {
                    new() {
                        RoleId = rrp.RoleId,
                        ReportsPermissionId = rrp.ReportsPermissionId,
                        CanEdit = rrp.CanEdit,
                        CanSign = rrp.CanSign
                    }
                }
            }).FirstOrDefaultAsync() ?? new UserPermissions();
    }
}