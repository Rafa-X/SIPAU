using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Queries;

public static class SecurityQueries
{
    public static async Task<SystemUser> GetSystemUserAsync(this AppDBContext db, string email)
    {
        var result = 
        from sy in db.SystemUsers.AsNoTracking()
        join ur in db.UserRoles.AsNoTracking() on sy.Id equals ur.UserId
        join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
        where sy.Email == email && sy.IsActive
        select new SystemUser
        {
            Id = sy.Id,
            EmployeeId = sy.EmployeeId,
            Email = sy.Email,
            Password = sy.Password,
            IsActive = sy.IsActive,
            LastLogin = sy.LastLogin,
            Permissions = new Permissions
            {
                Roles = new List<Role> { 
                    new()
                    {
                        Id = r.Id,
                        Name = r.Name
                    }
                }
            }
        };

        return await result.FirstOrDefaultAsync() ?? new SystemUser();
    }

    public static async Task<SystemUser> GetSystemUserAsync(this AppDBContext db, Guid userId)
    {
        var result = 
        from sy in db.SystemUsers.AsNoTracking()
        join ur in db.UserRoles.AsNoTracking() on sy.Id equals ur.UserId
        join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
        where sy.Id == userId && sy.IsActive
        select new SystemUser
        {
            Id = sy.Id,
            EmployeeId = sy.EmployeeId,
            Email = sy.Email,
            Password = sy.Password,
            IsActive = sy.IsActive,
            LastLogin = sy.LastLogin,
            Permissions = new Permissions
            {
                Roles = new List<Role> {
                    new()
                    {
                        Id = r.Id,
                        Name = r.Name
                    }
                }
            }
        };

        return await result.FirstOrDefaultAsync() ?? new SystemUser();
    }

    public static async Task<Permissions> GetSystemUserPermissionsAsync(this AppDBContext db, Guid userId)
    {
        var result = 
        from sy in db.SystemUsers.AsNoTracking()
        join ur in db.UserRoles.AsNoTracking() on sy.Id equals ur.UserId
        join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
        join rp in db.ReportsPermissions.AsNoTracking() on r.Id equals rp.RoleId
        where sy.Id == userId && sy.IsActive
        select new Permissions
        {
            Roles = new List<Role> { 
                new()
                {
                    Id = r.Id,
                    Name = r.Name
                }
                },
            ReportsPermissions = new List<ReportsPermission>{
                new() {
                    Id = rp.Id,
                    RoleId = rp.RoleId,
                    ReportName = rp.ReportName,
                    CanEdit = rp.CanEdit,
                    CanSign = rp.CanSign
                }
            }
        };

        return result.FirstOrDefault() ?? new Permissions();
    }
}