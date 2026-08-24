using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class SecurityRepository(AppDBContext db)
{
    public async Task<UserResponse> CheckPermissions(Guid? requesterKey, List<string> required)
    {
        if(requesterKey == null || requesterKey == Guid.Empty)
            throw new ArgumentException("Requester key cannot be null or empty.", nameof(requesterKey));

        var user = await GetSystemUserPermissionsAsync(requesterKey.Value);
        var response = new UserResponse();

        if(!user.Permissions.Any(p => required.Contains(p.Name)))
        {
            response.StatusMessage = "Access Denied: User does not have the required permissions.";
            return response;
        }

        response.Success = true;
        return response;
    }

    public async Task<UserPermissions> GetSystemUserPermissionsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return new UserPermissions();

        return await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            join rp in db.RolePermissions.AsNoTracking() on r.Id equals rp.RoleId
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            join rrp in db.RoleReportsPermissions.AsNoTracking() on rp.RoleId equals rrp.RoleId
            where ur.UserId == userId && rp.RoleId == r.Id && rrp.RoleId == r.Id
            select new UserPermissions
            {
                Permissions = new List<Permission>
                {
                    new()
                    {
                        Id = p.Id,
                        Name = p.Name
                    }
                },
                ReportsPermissions = new List<RoleReportPermission>
                {
                    new()
                    {
                        RoleId = rrp.RoleId,
                        ReportsPermissionId = rrp.ReportsPermissionId,
                        CanEdit = rrp.CanEdit,
                        CanSign = rrp.CanSign
                    }
                }
            })
            .FirstOrDefaultAsync() ?? new UserPermissions();
    }
}