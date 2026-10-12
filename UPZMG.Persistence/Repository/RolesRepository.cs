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
        var name = request.Name.Trim();
        if (name.Length == 0) throw new InvalidOperationException("El rol necesita un nombre.");

        // Two roles with one name cannot be told apart in the listing, and the screens that
        // still read a role name would match both.
        if (await db.Roles.AnyAsync(r => r.Name == name))
            throw new InvalidOperationException($"Ya existe un rol llamado «{name}».");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return role;
    }

    public async Task<Role> EditRoleAsync(EditRole request)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId)
            ?? throw new Exception($"No role found with ID {request.RoleId}");

        var name = request.Name.Trim();
        if (name.Length == 0) throw new InvalidOperationException("El rol necesita un nombre.");

        // Authorisation is declared by role name, so renaming Admin would leave every
        // administrative endpoint unreachable and the delete guard blind to it.
        if (role.Name == "Admin" && name != "Admin")
            throw new InvalidOperationException("El rol Admin no puede renombrarse: la autorización del sistema depende de su nombre.");

        if (await db.Roles.AnyAsync(r => r.Id != role.Id && r.Name == name))
            throw new InvalidOperationException($"Ya existe un rol llamado «{name}».");

        role.Name = name;
        await db.SaveChangesAsync();

        return role;
    }

    public async Task DeleteRoleAsync(DeleteRole request)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId)
            ?? throw new Exception($"No role found with ID {request.RoleId}");

        if (role.Name == "Admin")
            throw new InvalidOperationException("El rol Admin no puede eliminarse.");

        // Deleting a role takes its permissions away from everyone in it. If this is the only
        // role with an active administrator, there would be nobody left to grant them back.
        if (await GrantsAdministrationAsync(request.RoleId)
            && !await AnotherActiveAdministratorExistsAsync(request.RoleId))
        {
            throw new InvalidOperationException(
                "Es el único rol con una cuenta activa que puede administrar usuarios: "
                + "eliminarlo dejaría el sistema sin administración.");
        }

        db.UserRoles.RemoveRange(db.UserRoles.Where(ur => ur.RoleId == request.RoleId));
        db.RolePermissions.RemoveRange(db.RolePermissions.Where(rp => rp.RoleId == request.RoleId));
        db.RoleReportsPermissions.RemoveRange(db.RoleReportsPermissions.Where(rrp => rrp.RoleId == request.RoleId));
        db.Roles.Remove(role);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Whether some role other than <paramref name="excludedRoleId"/> both grants
    /// "administrar usuarios" and has at least one active account in it.
    /// </summary>
    private async Task<bool> GrantsAdministrationAsync(Guid roleId) =>
        await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where rp.RoleId == roleId && p.Name == AppPermissions.ManageUsers
            select rp.Id)
            .AnyAsync();

    private async Task<bool> AnotherActiveAdministratorExistsAsync(Guid excludedRoleId) =>
        await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            join ur in db.UserRoles.AsNoTracking() on rp.RoleId equals ur.RoleId
            join u in db.SystemUsers.AsNoTracking() on ur.UserId equals u.Id
            where p.Name == AppPermissions.ManageUsers && rp.RoleId != excludedRoleId && u.IsActive
            select u.Id)
            .AnyAsync();

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
            var thisRoleHadIt = await db.RolePermissions.AsNoTracking()
                .AnyAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == manageUsers);

            // Another role holding the permission is not enough: it also needs somebody active
            // in it. A role nobody can sign in to administers nothing.
            if (thisRoleHadIt && !await AnotherActiveAdministratorExistsAsync(request.RoleId))
                throw new InvalidOperationException(
                    "Es el único rol con una cuenta activa que puede administrar usuarios: "
                    + "quitarle ese permiso dejaría el sistema sin administración.");
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
