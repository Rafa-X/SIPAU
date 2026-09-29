using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;
using UPZMG.Implementations;
using UPZMG.Implementations.Roles;

using UPZMG.Implementations.Security;

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

        // Authorisation is declared by role name, so renaming Admin would leave every
        // administrative endpoint unreachable and the delete guard blind to it.
        if (role.Name == "Admin" && request.Name != "Admin")
            throw new InvalidOperationException("El rol Admin no puede renombrarse: la autorización del sistema depende de su nombre.");

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
        // Withdrawing the last grant of "administrar usuarios" would leave the permissions screen
        // unreachable for everyone, including whoever is using it right now.
        var administerId = await db.Permissions.AsNoTracking()
            .Where(p => p.Name == AppPermissions.ManageUsers)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();

        if (administerId is { } manageUsers && !request.PermissionIds.Contains(manageUsers))
        {
            var othersKeepIt = await db.RolePermissions.AsNoTracking()
                .AnyAsync(rp => rp.RoleId != request.RoleId && rp.PermissionId == manageUsers);

            var thisRoleHadIt = await db.RolePermissions.AsNoTracking()
                .AnyAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == manageUsers);

            if (thisRoleHadIt && !othersKeepIt)
                throw new InvalidOperationException(
                    "Es el único rol que puede administrar usuarios: quitarle ese permiso dejaría el sistema sin administración.");
        }

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
