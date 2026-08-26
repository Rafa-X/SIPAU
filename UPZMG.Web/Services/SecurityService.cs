using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace UPZMG.Web.Services;

public sealed class SecurityService(IHttpClientFactory httpClientFactory, ApiTokenService apiTokenService, AuthenticationStateProvider authState)
{
    public Task<string> GetTokenAsync(Guid userId)
    {
        return apiTokenService.GetTokenAsync(userId);
    }

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

    public class DataResponse
    {
        public string? Result { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public HttpResponseMessage? HttpResponse { get; set; }
        public bool Sucess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}