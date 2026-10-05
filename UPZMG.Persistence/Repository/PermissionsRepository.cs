using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;
using UPZMG.Implementations.Permissions;

namespace UPZMG.Persistence.Repository;

public class PermissionsRepository(AppDBContext _db)
{
    private readonly AppDBContext db = _db;

    public async Task<List<Permission>> GetAllPermissionsAsync(GetAllPermissions request)
    {
        return await db.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Select(permission => new Permission
            {
                Id = permission.Id,
                Name = permission.Name,
                // The screen lists these: a slug alone does not tell an administrator what it grants.
                DisplayName = permission.DisplayName,
                Description = permission.Description,
            })
            .ToListAsync();
    }

    public async Task<Permission> CreatePermissionAsync(CreatePermission request)
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        };

        db.Permissions.Add(permission);
        await db.SaveChangesAsync();

        return permission;
    }

    public async Task<Permission> EditPermissionAsync(EditPermission request)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId)
            ?? throw new Exception($"No permission found with ID {request.PermissionId}");

        permission.Name = request.Name;
        await db.SaveChangesAsync();

        return permission;
    }

    public async Task DeletePermissionAsync(DeletePermission request)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId)
            ?? throw new Exception($"No permission found with ID {request.PermissionId}");

        db.RolePermissions.RemoveRange(db.RolePermissions.Where(rp => rp.PermissionId == request.PermissionId));
        db.Permissions.Remove(permission);
        await db.SaveChangesAsync();
    }
}
