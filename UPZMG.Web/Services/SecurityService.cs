using System.Net;
using System.Net.Http.Headers;

using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models.Security;

namespace UPZMG.Web.Services;

public sealed class SecurityService(IHttpClientFactory httpClientFactory, ApiTokenService apiTokenService, AppDBContext db)
{
    public Task<string> GetTokenAsync(Guid userId)
    {
        return apiTokenService.GetTokenAsync(userId);
    }

    public async Task<DataResponse> SendPostRequestAsync<TRequest, TResponse>(string uri, string json, Guid? userId = null)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{uri}")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        try
        {
            var client = await CreateClientAsync(userId);
            var response = await client.SendAsync(httpRequestMessage);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return new DataResponse
                {
                    Result = content,
                    StatusCode = response.StatusCode,
                    HttpResponse = response,
                    Sucess = true
                };
            }
            else
            {
                return new DataResponse
                {
                    StatusCode = response.StatusCode,
                    Sucess = false,
                    ErrorMessage = $"Request failed with status code: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            return new DataResponse
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Sucess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DataResponse> SendGetRequestAsync<TResponse>(
        string path,
        Guid? userId = null,
        bool returnNullOnUnauthorized = false,
        CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(userId);
        var response = await client.GetAsync(path, cancellationToken);

        if (returnNullOnUnauthorized && response.StatusCode == HttpStatusCode.Unauthorized)
            return new DataResponse
            {
                StatusCode = response.StatusCode,
                Sucess = false,
                ErrorMessage = "Unauthorized"
            };

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        return new DataResponse
        {
            Result = result is not null ? System.Text.Json.JsonSerializer.Serialize(result) : null,
            StatusCode = response.StatusCode,
            HttpResponse = response,
            Sucess = true
        };
    }

    private async Task<HttpClient> CreateClientAsync(Guid? userId)
    {
        var client = httpClientFactory.CreateClient("UpzmgApi");

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var token = await apiTokenService.GetTokenAsync(userId.Value);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    #region methods to migrate
    private sealed record RolePerm(Guid RoleId, string RoleName, Guid PermissionId, string PermissionName);

    // Cached once per process. Without this, every page load fires a query that
    // EF logs at 'fail' level before our catch swallows it — noisy console for
    // a database where the security tables simply haven't been migrated yet.
    // OBJECT_ID returns NULL for a missing table instead of throwing.
    private static bool? _securityTablesExist;

    private async Task<bool> SecurityTablesExistAsync()
    {
        if (_securityTablesExist is { } cached) return cached;
        try
        {
            // One round-trip, materialised client-side so EF doesn't log an
            // "unordered FirstOrDefault" advisory for a scalar existence probe.
            var ids = await db.Database
                .SqlQueryRaw<int?>("SELECT OBJECT_ID('roles') AS Value UNION ALL SELECT OBJECT_ID('rolePermissions')")
                .ToListAsync();
            _securityTablesExist = ids.Count == 2 && ids.All(id => id is not null);
        }
        catch
        {
            _securityTablesExist = false; // DB unreachable
        }
        return _securityTablesExist.Value;
    }

    public async Task<SecurityData> LoadAsync(Guid? roleId)
    {
        if (!await SecurityTablesExistAsync())
            return new SecurityData(new(), roleId, string.Empty, new(), DbAvailable: false);

        try
        {
            var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

            var pairs = await (
                from rp in db.RolePermissions.AsNoTracking()
                join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                join r in db.Roles.AsNoTracking() on rp.RoleId equals r.Id
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
        if (!await SecurityTablesExistAsync())
            return "No se pudo actualizar el permiso (la tabla de permisos no está disponible).";

        try
        {
            var name = permissionName.Trim();
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role is null) return "Rol no encontrado.";

            var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Name == name);
            if (permission is null && enable)
            {
                permission = new Permission { Id = Guid.NewGuid(), Name = name };
                db.Permissions.Add(permission);
            }
            if (permission is null) return string.Empty;

            var existing = await db.RolePermissions.FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permission.Id);

            if (enable && existing is null)
            {
                db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
                await db.SaveChangesAsync();
                return $"Permiso '{name}' agregado al rol {role.Name}.";
            }
            if (!enable && existing is not null)
            {
                db.RolePermissions.Remove(existing);
                await db.SaveChangesAsync();
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

    public sealed record SecurityData(
    List<RoleListItemViewModel> Roles,
    Guid? SelectedRoleId,
    string SelectedRoleName,
    List<PermissionToggleViewModel> Permissions,
    bool DbAvailable);
    #endregion

    public class DataResponse
    {
        public string? Result { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public HttpResponseMessage? HttpResponse { get; set; }
        public bool Sucess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}