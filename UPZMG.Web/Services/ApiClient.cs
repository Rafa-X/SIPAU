using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using UPZMG.Persistence.Models;
using UPZMG.Web.Services;

namespace UPZMG.Web.Services;

public class ApiClient(
    IHttpClientFactory httpClientFactory,
    ApiTokenService tokenService,
    IMemoryCache cache,
    PermissionCacheEpoch epoch)
{
    private static readonly TimeSpan SuspendedCacheFor = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ApiTokenService _tokenService = tokenService;

    // Generic helper: prepare client with Bearer token
    private async Task<HttpClient> CreateAuthorizedClientAsync(Guid userId)
    {
        try
        {
        var token = await _tokenService.GetTokenAsync(userId);

        var client = _httpClientFactory.CreateClient("UpzmgApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating authorized API client: {ex.Message}");
            throw;
        }
    }

    public async Task<UserPermissions> GetPermissionsSnapshotAsync(Guid userId)
    {
        var client = await CreateAuthorizedClientAsync(userId);
        var result = await client.GetFromJsonAsync<UserPermissions>("auth/permissions");

        return result ?? new UserPermissions();
    }

    /// <summary>
    /// Whether the API reports this account as suspended. Both calls that can tell answer 403: the
    /// token exchange, and the permissions when a token is still cached. An API that does not answer
    /// says nothing about the account, so it counts as not suspended and nobody is signed out for it.
    /// </summary>
    public async Task<bool> IsSuspendedAsync(Guid userId, bool fresh = false)
    {
        var key = $"web_suspended:{userId}";
        if (!fresh && cache.TryGetValue<bool>(key, out var known)) return known;

        bool suspended;
        try
        {
            await GetPermissionsSnapshotAsync(userId);
            suspended = false;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            suspended = true;
        }
        catch (Exception)
        {
            return false;
        }

        cache.Set(key, suspended, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(SuspendedCacheFor)
            .AddExpirationToken(epoch.Token));
        return suspended;
    }
}
