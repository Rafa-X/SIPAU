using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models.Security;

namespace UPZMG.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SecurityController : Controller
{
    private readonly AppDBContext _db;

    private sealed record RolePermissions(Guid RoleId, string RoleName, Guid PermissionId, string PermissionName);

    public SecurityController(AppDBContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? roleId)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync();

        var PermissionsList = await (
            from rp in _db.RolePermissions.AsNoTracking()
            join p in _db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            join r in _db.Roles.AsNoTracking() on rp.RoleId equals r.Id
            select new RolePermissions(rp.RoleId, r.Name, p.Id, p.Name))
            .ToListAsync();

        var selectedRole = roleId.HasValue ? roles.FirstOrDefault(r => r.Id == roleId.Value) : null;

        var model = new SecurityPageViewModel
        {
            Roles = roles.Select(r => new RoleListItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                PermissionCount = PermissionsList.Count(x => x.RoleId == r.Id)
            }).ToList(),
            SelectedRoleId = selectedRole?.Id,
            SelectedRoleName = selectedRole?.Name ?? string.Empty,
            Permissions = BuildPermissionToggles(selectedRole, PermissionsList),
            StatusMessage = TempData["SecurityStatus"] as string
        };

        ApplySidebarUserInfo();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePermission(Guid roleId, string permissionName, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            return RedirectToAction(nameof(Index), new { roleId });

        var normalizedPermissionName = permissionName.Trim();

        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role is null)
            return RedirectToAction(nameof(Index));

        var permission = await _db.Permissions
            .FirstOrDefaultAsync(p => p.Name == normalizedPermissionName);

        if (permission is null && isEnabled)
        {
            permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = normalizedPermissionName
            };

            _db.Permissions.Add(permission);
        }

        if (permission is null)
            return RedirectToAction(nameof(Index), new { roleId });

        var existingPermission = await _db.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permission.Id);

        if (isEnabled && existingPermission is null)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
            TempData["SecurityStatus"] = $"Permiso '{normalizedPermissionName}' agregado al rol {role.Name}.";
        }

        if (!isEnabled && existingPermission is not null)
        {
            _db.RolePermissions.Remove(existingPermission);
            TempData["SecurityStatus"] = $"Permiso '{normalizedPermissionName}' removido del rol {role.Name}.";
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { roleId });
    }

    private static List<PermissionToggleViewModel> BuildPermissionToggles(Role? selectedRole, List<RolePermissions> PermissionsList)
    {
        var names = PermissionsList.Select(x => x.PermissionName)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        var selectedPermissionNames = selectedRole is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : PermissionsList
                .Where(x => x.RoleId == selectedRole.Id)
                .Select(x => x.PermissionName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return names.Select(permissionName => new PermissionToggleViewModel
        {
            Name = permissionName ?? string.Empty,
            IsAssignedToSelectedRole = selectedPermissionNames.Contains(permissionName ?? string.Empty),
            AssignedRoleNames = PermissionsList
                .Where(x => string.Equals(x.PermissionName, permissionName, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList()
        }).ToList();
    }

    private void ApplySidebarUserInfo()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var localPart = email.Split('@')[0];
        var chunks = localPart.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (chunks.Length == 0)
            chunks = ["usuario"];

        var displayName = string.Join(" ", chunks.Select(c =>
            string.Concat(char.ToUpperInvariant(c[0]), c[1..])));

        var initials = string.Join(string.Empty, chunks.Select(c => char.ToUpperInvariant(c[0])));

        ViewData["UserName"] = displayName;
        ViewData["UserRole"] = "Admin";
        ViewData["UserInitials"] = initials;
    }
}
