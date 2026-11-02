using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace UPZMG.Web.Services;

/// <summary>
/// Service responsible for managing API tokens.
/// </summary>

public class ApiTokenService
{
    private readonly IHttpClientFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _cfg;
    private readonly ISecurityEventLogger _securityLogger;

    public ApiTokenService(IHttpClientFactory factory, IMemoryCache cache, IConfiguration cfg, ISecurityEventLogger securityLogger)
    {
        _factory = factory;
        _cache = cache;
        _cfg = cfg;
        _securityLogger = securityLogger;
    }

    private static string CacheKey(string subject) => $"api_token:{subject}";

    /// <summary>
    /// Drops the cached token the API has just refused. The token says whose it is in its payload,
    /// which is read here only to find the entry: it is the API that decides whether to trust it.
    /// </summary>
    public static void Forget(IMemoryCache cache, string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

            if (json.RootElement.TryGetProperty("sub", out var subject)
                && subject.GetString() is { } id
                && cache.TryGetValue<string>(CacheKey(id), out var cached)
                && cached == token)
            {
                cache.Remove(CacheKey(id));
            }
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            // Not a token this Web issued the request with; there is nothing to forget.
        }
    }

    public async Task<string> GetTokenAsync(Guid userId)
    {
        var cacheKey = CacheKey(userId.ToString());
        //Already have a valid token in cache, return it
        if (_cache.TryGetValue<string>(cacheKey, out var token) && !string.IsNullOrWhiteSpace(token))
        {
            _securityLogger.LogApiTokenCacheHit(userId);
            return token!;
        }

        try
        {
            _securityLogger.LogApiTokenRequest(userId);

            //If not, call the API to get a new token using the shared secret for authentication
            var client = _factory.CreateClient("UpzmgApi");
            var secret = _cfg["InternalAuth:WebSharedSecret"] ?? throw new InvalidOperationException("Missing shared secret.");

            var resp = await client.PostAsJsonAsync("auth/token", new { userId, sharedSecret = secret });
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
            token = body?.access_token ?? throw new InvalidOperationException("No token returned.");

            // Kept for its real lifetime less a minute, as the API reports it. A fixed 14 minutes
            // outlived any shorter lifetime an administrator configured, and every call then
            // failed with 401 until the cache entry expired.
            var lifetime = body?.expires_in is int seconds && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.FromMinutes(15);
            var keepFor = lifetime - TimeSpan.FromMinutes(1);
            if (keepFor < TimeSpan.FromSeconds(30)) keepFor = TimeSpan.FromSeconds(30);

            _cache.Set(cacheKey, token, keepFor);
            _securityLogger.LogApiTokenReceived(userId);
            return token;
        }
        catch (Exception ex)
        {
            _securityLogger.LogApiTokenRequestFailed(userId, ex);
            throw;
        }
    }

    private record TokenResponse(string access_token, int? expires_in = null);
}