using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;
using UPZMG.Implementations;
using UPZMG.Implementations.Roles;

namespace UPZMG.Persistence.Repository;

public class RolesRepository(AppDBContext _db)
{
    private readonly AppDBContext db = _db;

    public async Task<List<Role>> GetAllRolesAsync(GetAllRoles request)
    {
        return await db.Roles
            .AsNoTracking()
            .Select(role => new Role
            {
                Id = role.Id,
                Name = role.Name
            })
            .ToListAsync();
    }

    public async Task<Role> CreateRoleAsync(CreateRole request)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return role;
    }

    public async Task<Role> EditRoleAsync(EditRole request)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId)
            ?? throw new Exception($"No role found with ID {request.RoleId}");

        role.Name = request.Name;
        await db.SaveChangesAsync();

        return role;
    }

    public async Task DeleteRoleAsync(DeleteRole request)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId)
            ?? throw new Exception($"No role found with ID {request.RoleId}");

        if (role.Name == "Admin")
            throw new InvalidOperationException("The Admin role cannot be deleted.");

        db.UserRoles.RemoveRange(db.UserRoles.Where(ur => ur.RoleId == request.RoleId));
        db.RolePermissions.RemoveRange(db.RolePermissions.Where(rp => rp.RoleId == request.RoleId));
        db.RoleReportsPermissions.RemoveRange(db.RoleReportsPermissions.Where(rrp => rrp.RoleId == request.RoleId));
        db.Roles.Remove(role);
        await db.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetRolePermissionIdsAsync(GetRolePermissions request)
    {
        return await db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == request.RoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task SetRolePermissionsAsync(SetRolePermissions request)
    {
        var current = await db.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync();

        db.RolePermissions.RemoveRange(current);

        foreach (var permissionId in request.PermissionIds.Distinct())
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = request.RoleId,
                PermissionId = permissionId
            });
        }

        await db.SaveChangesAsync();
    }
}
