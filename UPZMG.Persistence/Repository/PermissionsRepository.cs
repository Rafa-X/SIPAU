using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;
using UPZMG.Implementations.Permissions;
using UPZMG.Implementations.Security;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// The permission catalogue is declared in code (<see cref="AppPermissions"/>) because every
/// authorisation check compares against those exact names. This repository keeps the table in
/// sync with that declaration; what an administrator really decides is which roles hold them.
/// </summary>
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

    /// <summary>
    /// Adds the catalogue permissions the table is missing and refreshes the wording of the
    /// ones it has. Role assignments are never touched, so re-running this cannot revoke what
    /// an administrator granted — which is what made re-running the seed unsafe.
    /// </summary>
    public async Task<CatalogueRestoreResult> RestoreCatalogueAsync()
    {
        var existing = await db.Permissions.ToListAsync();
        int added = 0, updated = 0;

        foreach (var definition in AppPermissions.All)
        {
            var row = existing.FirstOrDefault(p => p.Name == definition.Name);

            if (row is null)
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    DisplayName = definition.DisplayName,
                    Description = definition.Description,
                });
                added++;
            }
            else if (row.DisplayName != definition.DisplayName || row.Description != definition.Description)
            {
                row.DisplayName = definition.DisplayName;
                row.Description = definition.Description;
                updated++;
            }
        }

        if (added > 0 || updated > 0) await db.SaveChangesAsync();

        var unknown = existing.Count(p => !IsDeclared(p.Name));
        return new CatalogueRestoreResult(added, updated, unknown);
    }

    /// <summary>Restores one catalogue permission; a name the system does not check is refused.</summary>
    public async Task<Permission> CreatePermissionAsync(CreatePermission request)
    {
        var name = request.Name.Trim();

        var definition = AppPermissions.All.FirstOrDefault(p => p.Name == name)
            ?? throw new InvalidOperationException(
                $"«{name}» no es un permiso del sistema: ninguna pantalla ni endpoint lo revisa, "
                + "así que crearlo no otorgaría nada. El catálogo lo define el sistema.");

        if (await db.Permissions.AnyAsync(p => p.Name == name))
            throw new InvalidOperationException($"El permiso «{name}» ya existe.");

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = definition.Name,
            DisplayName = definition.DisplayName,
            Description = definition.Description,
        };

        db.Permissions.Add(permission);
        await db.SaveChangesAsync();

        return permission;
    }

    /// <summary>
    /// Renames a leftover permission. A catalogue one cannot be renamed: the name is the
    /// contract the code checks, so renaming "usuarios.administrar" would lock every
    /// administrator out of the screen they are standing on.
    /// </summary>
    public async Task<Permission> EditPermissionAsync(EditPermission request)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId)
            ?? throw new InvalidOperationException($"El permiso ya no existe.");

        EnsureNotDeclared(permission.Name, "renombrarse");

        var name = request.Name.Trim();
        if (name.Length == 0) throw new InvalidOperationException("El nombre del permiso no puede quedar vacío.");

        if (await db.Permissions.AnyAsync(p => p.Id != permission.Id && p.Name == name))
            throw new InvalidOperationException($"Ya existe un permiso llamado «{name}».");

        permission.Name = name;
        await db.SaveChangesAsync();

        return permission;
    }

    /// <summary>Removes a leftover permission; a catalogue one is part of the system.</summary>
    public async Task DeletePermissionAsync(DeletePermission request)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId)
            ?? throw new InvalidOperationException($"El permiso ya no existe.");

        EnsureNotDeclared(permission.Name, "eliminarse");

        db.RolePermissions.RemoveRange(db.RolePermissions.Where(rp => rp.PermissionId == request.PermissionId));
        db.Permissions.Remove(permission);
        await db.SaveChangesAsync();
    }

    private static bool IsDeclared(string? name) => AppPermissions.All.Any(p => p.Name == name);

    private static void EnsureNotDeclared(string? name, string action)
    {
        if (IsDeclared(name))
            throw new InvalidOperationException(
                $"«{name}» es un permiso del sistema y no puede {action}: la autorización compara ese "
                + "nombre exacto. Lo que se administra es qué roles lo tienen.");
    }
}
