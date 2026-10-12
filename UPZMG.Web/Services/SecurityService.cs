using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using UPZMG.Implementations;
using UPZMG.Implementations.Permissions;
using UPZMG.Implementations.Roles;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public sealed class SecurityService(
    IHttpClientFactory httpClientFactory, 
    ApiTokenService apiTokenService, 
    AuthenticationStateProvider authState) : ISecurityService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public Task<string> GetTokenAsync(Guid userId)
    {
        return apiTokenService.GetTokenAsync(userId);
    }

#region Account
    public async Task<AccountWebLoginResponse> AuthenticateUserAsync(AccountWebLoginRequest request)
    {
        var response = await SendPostRequestAsync<AccountWebLoginRequest, AccountWebLoginResponse>(
            $"auth/web-login", JsonSerializer.Serialize(request));

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<AccountWebLoginResponse>(response.Result, ResponseOptions) ?? new AccountWebLoginResponse();
        }

        // Telling someone their credentials are wrong when the API is only asking them to wait
        // sends them to reset a password that was never the problem.
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var wait = response.HttpResponse?.Headers.RetryAfter?.Delta?.TotalSeconds;
            return new AccountWebLoginResponse
            {
                Throttled = true,
                RetryAfterSeconds = (int)Math.Ceiling(wait ?? 300),
            };
        }

        return new AccountWebLoginResponse();
    }
#endregion

#region GetPost Requests

    /// <summary>Resolves the current user's id claim as a <see cref="Guid"/>, or <see cref="Guid.Empty"/> if unavailable.</summary>
    public async Task<Guid> GetRequesterKeyAsync()
    {
        var state = await authState.GetAuthenticationStateAsync();
        var value = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var requesterKey) ? requesterKey : Guid.Empty;
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
                var errorContent = await response.Content.ReadAsStringAsync();
                return new DataResponse
                {
                    StatusCode = response.StatusCode,
                    // Kept on failures too: the caller may need a header, such as Retry-After.
                    HttpResponse = response,
                    Sucess = false,
                    ErrorMessage = ExtractErrorMessage(errorContent) ?? $"Request failed with status code: {response.StatusCode}"
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

    // Reads the {"message": "..."} shape returned by Conflict(new { message }) responses.
    private static string? ExtractErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String
                ? messageProp.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
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

    public class DataResponse
    {
        public string? Result { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public HttpResponseMessage? HttpResponse { get; set; }
        public bool Sucess { get; set; }
        public string? ErrorMessage { get; set; }
    }
#endregion

#region Roles
    public async Task<List<Role>> GetAllRoles(GetAllRoles request)
    {
        var response = await SendPostRequestAsync<GetAllRoles, List<Role>>(
            $"roles/get-all", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<Role>>(response.Result, ResponseOptions) ?? new List<Role>();
        }
        return [];
    }

    public async Task<Role> CreateRole(CreateRole request)
    {
        var response = await SendPostRequestAsync<CreateRole, Role>(
            $"roles/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Role>(response.Result, ResponseOptions) ?? new Role();
        }

        // Returning an empty role here is what let the screen announce a save that never happened.
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible crear el rol.");
    }

    public async Task<Role> EditRole(EditRole request)
    {
        var response = await SendPostRequestAsync<EditRole, Role>(
            $"roles/edit", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Role>(response.Result, ResponseOptions) ?? new Role();
        }

        // Returning an empty role here is what let the screen announce a save that never happened.
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar el rol.");
    }

    public async Task<bool> DeleteRole(DeleteRole request)
    {
        var response = await SendPostRequestAsync<DeleteRole, object>(
            "roles/delete", JsonSerializer.Serialize(request), request.RequesterKey);

        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<List<Guid>> GetRolePermissions(GetRolePermissions request)
    {
        var response = await SendPostRequestAsync<GetRolePermissions, List<Guid>>(
            $"roles/get-permissions", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<Guid>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<bool> SetRolePermissions(SetRolePermissions request)
    {
        var response = await SendPostRequestAsync<SetRolePermissions, object>(
            $"roles/set-permissions", JsonSerializer.Serialize(request), request.RequesterKey);

        if (response.StatusCode == HttpStatusCode.OK) return true;

        // The API refuses, for instance, withdrawing the last grant of "administrar usuarios";
        // swallowing that would announce a change the system did not make.
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar los permisos del rol.");
    }
#endregion

#region Permissions
    public async Task<List<Permission>> GetAllPermissions(GetAllPermissions request)
    {
        var response = await SendPostRequestAsync<GetAllPermissions, List<Permission>>(
            $"permissions/get-all", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<List<Permission>>(response.Result, ResponseOptions) ?? [];
        }
        return [];
    }

    public async Task<Permission> CreatePermission(CreatePermission request)
    {
        var response = await SendPostRequestAsync<CreatePermission, Permission>(
            $"permissions/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Permission>(response.Result, ResponseOptions) ?? new Permission();
        }

        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible crear el permiso.");
    }

    public async Task<Permission> EditPermission(EditPermission request)
    {
        var response = await SendPostRequestAsync<EditPermission, Permission>(
            $"permissions/edit", JsonSerializer.Serialize(request), request.RequesterKey);

        if(response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Permission>(response.Result, ResponseOptions) ?? new Permission();
        }

        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar el permiso.");
    }

    public async Task<CatalogueRestoreResult> RestorePermissionCatalogue(GetAllPermissions request)
    {
        var response = await SendPostRequestAsync<GetAllPermissions, CatalogueRestoreResult>(
            "permissions/restore-catalogue", JsonSerializer.Serialize(request), request.RequesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<CatalogueRestoreResult>(response.Result, ResponseOptions)
                ?? new CatalogueRestoreResult(0, 0, 0);
        }

        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible restaurar el catálogo de permisos.");
    }

    public async Task<bool> DeletePermission(DeletePermission request)
    {
        var response = await SendPostRequestAsync<DeletePermission, object>(
            "permissions/delete", JsonSerializer.Serialize(request), request.RequesterKey);

        return response.StatusCode == HttpStatusCode.OK;
    }
#endregion
}