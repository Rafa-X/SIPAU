using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UPZMG.IntegrationTests.Infrastructure;

/// <summary>
/// The real API — its controllers, filters, authentication and repositories — running inside the
/// test process against the run's database. Only the transport is in memory; every request goes
/// through the same pipeline a deployed API runs.
/// </summary>
public sealed class ApiHost(TestDatabase database) : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public TestDatabase Database { get; } = database;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": that would load the developer's user secrets, and with them the
        // connection string of the team's database.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", Database.ConnectionString);
        builder.UseSetting("Jwt:Key", Database.JwtKey);
        builder.UseSetting("InternalAuth:WebSharedSecret", Database.SharedSecret);

        // Several tests provoke refusals on purpose — duplicate keys, a trigger that throws —
        // and the API logs each one; the output would bury the results under them.
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
        builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "None");
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore.HttpsPolicy", "Error");
    }

    /// <summary>Signs in the way the Web does: credentials and shared secret, then the token exchange.</summary>
    public async Task<ApiSession> SignInAsync(string email, string password)
    {
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("auth/web-login",
            new { email, password, sharedSecret = Database.SharedSecret });
        login.EnsureSuccessStatusCode();
        var user = (await login.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("userId").GetGuid();

        var exchange = await client.PostAsJsonAsync("auth/token", new { userId = user, sharedSecret = Database.SharedSecret });
        exchange.EnsureSuccessStatusCode();
        var token = (await exchange.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("access_token").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new ApiSession(client, user, email);
    }
}

/// <summary>A signed-in caller: an HTTP client that carries the account's token.</summary>
public sealed record ApiSession(HttpClient Client, Guid UserId, string Email)
{
    public Task<HttpResponseMessage> PostAsync(string path, object body) =>
        Client.PostAsJsonAsync(path, body, ApiHost.Json);

    public async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(ApiHost.Json))!;
}

/// <summary>The accounts test-data.sql creates, one per area.</summary>
public static class Accounts
{
    public const string Admin = "admin@upzmg.local";
    public const string ControlEscolar = "control.escolar@upzmg.local";
    public const string ServiciosEstudiantiles = "servicios.estudiantiles@upzmg.local";
    public const string RecursosHumanos = "recursos.humanos@upzmg.local";
    public const string DireccionAcademica = "docente_01@upzmg.local";
    public const string RecursosMateriales = "recursos.materiales@upzmg.local";

    public static string PasswordOf(string email) => email switch
    {
        Admin => "Admin123!",
        DireccionAcademica => "Teacher123!",
        _ => "User123!",
    };
}
