using System.Net.Http.Headers;
using System.Net.Http.Json;
using UPZMG.Persistence.Models;
using UPZMG.Web.Services;

namespace UPZMG.Web.Services;

public class ApiClient(IHttpClientFactory httpClientFactory, ApiTokenService tokenService)
{
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
}
