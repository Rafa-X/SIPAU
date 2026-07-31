using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models.Security;

namespace UPZMG.Web.Services;

/// <summary>
/// Roles &amp; permissions management (admin). Backed by the real database
/// (roles / permissions / role_permissions). All DB access is wrapped so a
/// missing table degrades to an empty state instead of a 500.
/// </summary>
public interface ISecurityService
{
    Task<SecurityData> LoadAsync(Guid? roleId);
    Task<string> ToggleAsync(Guid roleId, string permissionName, bool enable);
}

public sealed record SecurityData(
    List<RoleListItemViewModel> Roles,
    Guid? SelectedRoleId,
    string SelectedRoleName,
    List<PermissionToggleViewModel> Permissions,
    bool DbAvailable);

public sealed class SecurityService : ISecurityService
{
    private readonly AppDBContext _db;
    public SecurityService(AppDBContext db) => _db = db;

    private sealed record RolePerm(Guid RoleId, string RoleName, Guid PermissionId, string PermissionName);

    public async Task<SecurityData> LoadAsync(Guid? roleId)
    {
        try
        {
            var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

            var pairs = await (
                from rp in _db.RolePermissions.AsNoTracking()
                join p in _db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                join r in _db.Roles.AsNoTracking() on rp.RoleId equals r.Id
                select new RolePerm(rp.RoleId, r.Name, p.Id, p.Name)).ToListAsync();

            var selectedRole = roleId.HasValue ? roles.FirstOrDefault(r => r.Id == roleId.Value) : null;

            var roleItems = roles.Select(r => new RoleListItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                PermissionCount = pairs.Count(x => x.RoleId == r.Id),
            }).ToList();

            return new SecurityData(roleItems, selectedRole?.Id, selectedRole?.Name ?? string.Empty,
                BuildToggles(selectedRole, pairs), DbAvailable: true);
        }
        catch (Exception)
        {
            // Table(s) not present yet — surface a graceful empty state.
            return new SecurityData(new(), roleId, string.Empty, new(), DbAvailable: false);
        }
    }

    public async Task<string> ToggleAsync(Guid roleId, string permissionName, bool enable)
    {
        try
        {
            var name = permissionName.Trim();
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role is null) return "Rol no encontrado.";

            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Name == name);
            if (permission is null && enable)
            {
                permission = new Permission { Id = Guid.NewGuid(), Name = name };
                _db.Permissions.Add(permission);
            }
            if (permission is null) return string.Empty;

            var existing = await _db.RolePermissions.FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permission.Id);

            if (enable && existing is null)
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
                await _db.SaveChangesAsync();
                return $"Permiso '{name}' agregado al rol {role.Name}.";
            }
            if (!enable && existing is not null)
            {
                _db.RolePermissions.Remove(existing);
                await _db.SaveChangesAsync();
                return $"Permiso '{name}' removido del rol {role.Name}.";
            }
            return string.Empty;
        }
        catch (Exception)
        {
            return "No se pudo actualizar el permiso (la tabla de permisos no está disponible).";
        }
    }

    private static List<PermissionToggleViewModel> BuildToggles(Role? selectedRole, List<RolePerm> pairs)
    {
        var names = pairs.Select(x => x.PermissionName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selected = selectedRole is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : pairs.Where(x => x.RoleId == selectedRole.Id).Select(x => x.PermissionName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return names.Select(n => new PermissionToggleViewModel
        {
            Name = n,
            IsAssignedToSelectedRole = selected.Contains(n),
            AssignedRoleNames = pairs.Where(x => string.Equals(x.PermissionName, n, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.RoleName).Distinct().OrderBy(x => x).ToList(),
        }).ToList();
    }
}
