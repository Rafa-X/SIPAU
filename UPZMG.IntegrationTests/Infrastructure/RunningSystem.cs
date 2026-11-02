using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace UPZMG.IntegrationTests.Infrastructure;

/// <summary>
/// The API and the Web started as two operating-system processes, each on its own port, talking
/// over HTTP exactly as they do when deployed: the Web holds the user's cookie and asks the API,
/// which holds the database, for a token and for every figure. Either one can be stopped and
/// started again while the other keeps running.
///
/// The applications are taken from <see cref="ApiDirVariable"/> and <see cref="WebDirVariable"/>
/// (a publish folder each) or, when those are not set, from each project's own build output.
/// </summary>
public sealed partial class RunningSystem(TestDatabase database) : IAsyncDisposable
{
    public const string ApiDirVariable = "UPZMG_IT_API_DIR";
    public const string WebDirVariable = "UPZMG_IT_WEB_DIR";

    private Process? _api;
    private Process? _web;

    public int ApiPort { get; } = FreePort();
    public int WebPort { get; } = FreePort();

    public Uri ApiAddress => new($"http://127.0.0.1:{ApiPort}/");
    public Uri WebAddress => new($"https://127.0.0.1:{WebPort}/");

    public static string? ApiDirectory => Locate(ApiDirVariable, "UPZMG.API");
    public static string? WebDirectory => Locate(WebDirVariable, "UPZMG.Web");

    public async Task StartAsync()
    {
        await StartApiAsync();

        // The Web gets the API's address and the secret they share, and no connection string:
        // it has no way into the database, so everything these tests see went through the API.
        _web = Launch(WebDirectory!, "UPZMG.Web.dll", "web", new()
        {
            ["ASPNETCORE_URLS"] = WebAddress.ToString().TrimEnd('/'),
            ["Api__BaseAddress"] = ApiAddress.ToString(),
            ["InternalAuth__WebSharedSecret"] = database.SharedSecret,
        });
        await WaitUntilAnsweringAsync(WebAddress, _web, "web");
    }

    public async Task StartApiAsync()
    {
        _api = Launch(ApiDirectory!, "UPZMG.API.dll", "api", new()
        {
            ["ASPNETCORE_URLS"] = ApiAddress.ToString().TrimEnd('/'),
            ["ConnectionStrings__Default"] = database.ConnectionString,
            ["Jwt__Key"] = database.JwtKey,
            ["InternalAuth__WebSharedSecret"] = database.SharedSecret,
        });
        await WaitUntilAnsweringAsync(ApiAddress, _api, "api");
    }

    public async Task StopApiAsync()
    {
        if (_api is null) return;
        await StopAsync(_api);
        _api = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_web is not null) await StopAsync(_web);
        if (_api is not null) await StopAsync(_api);
    }

    /// <summary>A browser-like client for the Web: it keeps cookies and does not follow redirects.</summary>
    public HttpClient Browser(CookieContainer cookies, bool followRedirects = false) =>
        new(new HttpClientHandler
        {
            CookieContainer = cookies,
            AllowAutoRedirect = followRedirects,
            // The Web serves the development certificate; this client is not a browser to protect.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        { BaseAddress = WebAddress, Timeout = TimeSpan.FromSeconds(120) };

    /// <summary>Signs in through the Web's own form, antiforgery token included, as a person does.</summary>
    public async Task<HttpResponseMessage> SignInAsync(HttpClient browser, string email, string password, string? returnUrl = null)
    {
        var form = await browser.GetStringAsync("login");
        var token = AntiforgeryInput().Match(form) is { Success: true } input
            ? InputValue().Match(input.Value).Groups[1].Value
            : throw new InvalidOperationException("La página de inicio de sesión no trae el token antifalsificación.");

        var fields = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
        };
        if (returnUrl is not null) fields["ReturnUrl"] = returnUrl;

        return await browser.PostAsync("Account/Login", new FormUrlEncodedContent(fields));
    }

    private static Process Launch(string directory, string dll, string name, Dictionary<string, string> environment)
    {
        var start = new ProcessStartInfo("dotnet", $"\"{Path.Combine(directory, dll)}\"")
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // "Testing" keeps the developer's user secrets, and the team's database, out of the run.
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        foreach (var (key, value) in environment) start.Environment[key] = value;

        var process = Process.Start(start) ?? throw new InvalidOperationException($"No se pudo iniciar {name}.");
        var log = new StringBuilder();
        process.OutputDataReceived += (_, e) => { lock (log) log.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { lock (log) log.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        Logs[process] = log;
        return process;
    }

    private static readonly Dictionary<Process, StringBuilder> Logs = [];

    private static async Task WaitUntilAnsweringAsync(Uri address, Process process, string name)
    {
        using var probe = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        }) { Timeout = TimeSpan.FromSeconds(5) };

        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"El proceso {name} terminó al arrancar:\n{Tail(process)}");

            try
            {
                await probe.GetAsync(address);
                return; // Any answer, even a 404 or a redirect, means it is listening.
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }

            await Task.Delay(300);
        }

        throw new TimeoutException($"El proceso {name} no respondió en {address}:\n{Tail(process)}");
    }

    private static async Task StopAsync(Process process)
    {
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        process.Dispose();
    }

    private static string Tail(Process process)
    {
        if (!Logs.TryGetValue(process, out var log)) return string.Empty;
        lock (log) return string.Join('\n', log.ToString().Split('\n').TakeLast(30));
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string? Locate(string variable, string project)
    {
        if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } configured)
            return File.Exists(Path.Combine(configured, project + ".dll")) ? configured : null;

        // Next to the solution, in the project's own build output for this configuration.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "UPZMG.sln"))) directory = directory.Parent;
        if (directory is null) return null;

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        var output = Path.Combine(directory.FullName, project, "bin", configuration, "net10.0");
        return File.Exists(Path.Combine(output, project + ".dll")) ? output : null;
    }

    [GeneratedRegex("<input[^>]*name=\"__RequestVerificationToken\"[^>]*>")]
    private static partial Regex AntiforgeryInput();

    [GeneratedRegex("value=\"([^\"]*)\"")]
    private static partial Regex InputValue();
}

/// <summary>A test that starts the API and the Web as processes; skipped, saying why, when it cannot.</summary>
public sealed class DistributedFactAttribute : FactAttribute
{
    public DistributedFactAttribute()
    {
        if (TestDatabase.Server is null)
            Skip = $"Define {TestDatabase.ServerVariable} con la cadena de conexión de un SQL Server donde se puedan crear bases.";
        else if (RunningSystem.ApiDirectory is null || RunningSystem.WebDirectory is null)
            Skip = $"Compila la API y la Web, o define {RunningSystem.ApiDirVariable} y {RunningSystem.WebDirVariable} con sus carpetas publicadas.";
    }
}
