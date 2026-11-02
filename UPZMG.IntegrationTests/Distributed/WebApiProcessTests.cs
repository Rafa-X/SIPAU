using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UPZMG.IntegrationTests.Infrastructure;
using Xunit;

namespace UPZMG.IntegrationTests.Distributed;

/// <summary>
/// The Web and the API as the two separate programs they are in production, each in its own
/// process and port, communicating only over HTTP. What these tests show cannot be seen inside a
/// single process: that a person's session in the Web reaches the database through the API, and
/// what that person sees when the API stops and when it comes back.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Distributed")]
public sealed class WebApiProcessTests(TestDatabase database) : IAsyncLifetime
{
    private RunningSystem _system = null!;

    public async Task InitializeAsync()
    {
        if (RunningSystem.ApiDirectory is null || RunningSystem.WebDirectory is null || TestDatabase.Server is null) return;

        _system = new RunningSystem(database);
        await _system.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_system is not null) await _system.DisposeAsync();
    }

    [DistributedFact]
    public async Task ASessionOpenedInTheWebReadsTheDatabaseThroughTheApi()
    {
        var cookies = new CookieContainer();
        using var browser = _system.Browser(cookies);

        // Wrong credentials: the Web asks the API, the API says no, the Web goes back to the form.
        var refused = await _system.SignInAsync(browser, Accounts.Admin, "no-es-la-contraseña");
        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.StartsWith("/login?error", refused.Headers.Location!.OriginalString);

        var signedIn = await _system.SignInAsync(browser, Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        Assert.Equal("/dashboard", signedIn.Headers.Location!.OriginalString);
        Assert.Contains(cookies.GetCookies(_system.WebAddress).Cast<Cookie>(), c => c.Name == "UPZMG.Auth");

        // The page is rendered by the Web with figures the API computed from the database.
        var page = await browser.GetAsync("charts/dropout");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Alumnos evaluados", html);

        Assert.DoesNotContain("semilla", html);
    }

    [DistributedFact]
    public async Task TheRiskScreenSpeaksPlainlyToTheAreasThatActOnIt()
    {
        var cookies = new CookieContainer();
        using var browser = _system.Browser(cookies);
        using var following = _system.Browser(cookies, followRedirects: true);
        await _system.SignInAsync(browser, Accounts.ControlEscolar, "User123!");

        var html = await following.GetStringAsync("charts/dropout");

        Assert.Contains("Qué significa el porcentaje", html);
        Assert.DoesNotContain("pliegue", html);
        Assert.DoesNotContain("Gini", html);
        Assert.DoesNotContain("IC 95", html);
    }

    [DistributedFact]
    public async Task WithTheApiDownSigningInDoesNotBlameThePassword()
    {
        using var browser = _system.Browser(new CookieContainer());
        await _system.StopApiAsync();

        var refused = await _system.SignInAsync(browser, Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.Contains("error=servicio", refused.Headers.Location!.OriginalString);

        var page = await browser.GetStringAsync(refused.Headers.Location!.OriginalString.TrimStart('/'));
        Assert.Contains("no está disponible", page);
        Assert.DoesNotContain("Revisa tu correo y tu contraseña", page);

        await _system.StartApiAsync();
    }

    [DistributedFact]
    public async Task ASignInLandsOnTheScreenThatAskedForItAndNeverOnAnotherSite()
    {
        foreach (var (asked, landing) in new[]
        {
            ("/charts/dropout", "/charts/dropout"),
            ("https://otro-sitio.example/", "/dashboard"),
            ("//otro-sitio.example/", "/dashboard"),
            ("/no-encontrada", "/dashboard"),
        })
        {
            using var browser = _system.Browser(new CookieContainer());
            var signedIn = await _system.SignInAsync(browser, Accounts.Admin, Accounts.PasswordOf(Accounts.Admin), asked);

            Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
            Assert.Equal(landing, signedIn.Headers.Location!.OriginalString);
        }
    }

    [DistributedFact]
    public async Task ASuspendedAccountIsSignedOutAndToldWhy()
    {
        const string account = "recursos.materiales@upzmg.local";
        var cookies = new CookieContainer();
        using var browser = _system.Browser(cookies);
        using var following = _system.Browser(cookies, followRedirects: true);
        await _system.SignInAsync(browser, account, "User123!");

        using var api = new HttpClient { BaseAddress = _system.ApiAddress };
        var adminId = await SignInToApiAsync(api, Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
        var accountId = await SignInToApiAsync(api, account, "User123!");
        var token = await (await api.PostAsJsonAsync("auth/token", new { userId = adminId, sharedSecret = database.SharedSecret }))
            .Content.ReadFromJsonAsync<JsonElement>();
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("access_token").GetString());

        try
        {
            var suspend = await api.PostAsJsonAsync("users/set-active", new { requesterKey = adminId, userId = accountId, isActive = false });
            Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);

            var page = await following.GetAsync("dashboard");
            Assert.Contains("error=baja", page.RequestMessage!.RequestUri!.Query);
            Assert.Contains("dada de baja", await page.Content.ReadAsStringAsync());
            Assert.DoesNotContain(cookies.GetCookies(_system.WebAddress).Cast<Cookie>(), c => c.Name == "UPZMG.Auth");
        }
        finally
        {
            await api.PostAsJsonAsync("users/set-active", new { requesterKey = adminId, userId = accountId, isActive = true });
        }
    }

    private async Task<Guid> SignInToApiAsync(HttpClient api, string email, string password)
    {
        var response = await api.PostAsJsonAsync("auth/web-login", new { email, password, sharedSecret = database.SharedSecret });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
    }

    [DistributedFact]
    public async Task WhenTheApiStopsTheWebSaysSoAndRecoversWhenItReturns()
    {
        var cookies = new CookieContainer();
        using var browser = _system.Browser(cookies);
        using var following = _system.Browser(cookies, followRedirects: true);
        await _system.SignInAsync(browser, Accounts.ControlEscolar, "User123!");
        Assert.Contains("Alumnos evaluados", await following.GetStringAsync("charts/dropout"));

        await _system.StopApiAsync();

        // The Web stays up and the session stays open, but without the API it has nothing to
        // show and says so. While the permission it checked a moment ago is still remembered,
        // the page opens and reports that the model could not be read; once that expires, it
        // reports that the permissions could not be verified — never that the area has no access.
        var down = await following.GetAsync("charts/dropout");
        Assert.Equal(HttpStatusCode.OK, down.StatusCode);
        var downHtml = await down.Content.ReadAsStringAsync();
        Assert.True(downHtml.Contains("No se pudo consultar el riesgo") || downHtml.Contains("No pudimos verificar tus permisos"),
            "La página no explica que la API no respondió: " + Text(downHtml));
        Assert.DoesNotContain("Alumnos evaluados", downHtml);
        Assert.DoesNotContain("no está disponible para tu área", downHtml);

        await _system.StartApiAsync();

        // The Web remembers a failed check for a few seconds; past that, the same session works again.
        var recovered = false;
        for (var attempt = 0; attempt < 30 && !recovered; attempt++)
        {
            await Task.Delay(1000);
            recovered = (await following.GetStringAsync("charts/dropout")).Contains("Alumnos evaluados");
        }

        Assert.True(recovered, "La Web no volvió a mostrar el modelo después de que la API regresó.");
    }

    // The visible text of a page, for a failure message a person can read.
    private static string Text(string html) =>
        System.Text.RegularExpressions.Regex.Replace(
            System.Text.RegularExpressions.Regex.Replace(html, @"<(script|style)[^>]*>[\s\S]*?</\1>|<[^>]+>", " "),
            @"\s+", " ").Trim();
}
