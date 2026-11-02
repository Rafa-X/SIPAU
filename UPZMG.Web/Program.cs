using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using UPZMG.Web.Components;
using UPZMG.Web.Security;
using UPZMG.Web.Services;

// A service or a scheduled task has no console, and setting a title there stops the process
// before it starts.
try { Console.Title = "UPZMG.Web"; } catch (IOException) { }

var builder = WebApplication.CreateBuilder(args);
using var startupLoggerFactory = CreateStartupLoggerFactory();
var startupLogger = startupLoggerFactory.CreateLogger("Startup");

try
{
    startupLogger.LogInformation("Validating Web startup secrets.");

    ValidateRequiredSecret(builder.Configuration, "InternalAuth:WebSharedSecret", 32, startupLogger);

    builder.Services.AddControllersWithViews();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Blazor Web App (Interactive Server) + MudBlazor, coexisting with MVC.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices();

    // No database context here, on purpose: the Web holds no database credentials and every
    // read and write goes through the API. A context registered "just in case" was the only
    // reason a deployed Web would have needed a connection string.

    // The session cookie and the form tokens are protected with these keys. By default they are
    // tied to the folder the app runs from, so a new release folder or a second Web signed everybody
    // out. A second machine also has to share the keys: DataProtection:KeysPath points to that folder.
    var dataProtection = builder.Services.AddDataProtection().SetApplicationName("UPZMG.Web");
    if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

    // Cookie Auth
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "UPZMG.Auth";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = SessionPolicy.IdleTimeout;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    builder.Services.AddScoped<ApiTokenService>();
    builder.Services.AddScoped<ISecurityService, SecurityService>();
    builder.Services.AddScoped<ApiClient>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddSingleton<ISecurityEventLogger, SecurityLogger>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    // Scoped, not singleton: it now talks to the API per request/circuit.
    builder.Services.AddScoped<UsersService>();
    builder.Services.AddScoped<IDataCaptureService, DataCaptureService>();
    builder.Services.AddScoped<IStudentsService, StudentsService>();
    builder.Services.AddScoped<IGeneralDataService, GeneralDataService>();
    builder.Services.AddScoped<IPeriodsService, PeriodsService>();
    builder.Services.AddScoped<IBoardReportService, BoardReportService>();
    builder.Services.AddScoped<IReportsService, ReportsService>();
    // Scoped: the model runs in the API and each call carries the user's token.
    builder.Services.AddScoped<IIntelligenceService, IntelligenceService>();
    builder.Services.AddScoped<IChartsService, ChartsService>();

    builder.Services.AddAuthorization();

    // Screens are gated by permission, like the API endpoints, instead of by role name.
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, WebPermissionPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, WebPermissionHandler>();
    builder.Services.AddSingleton<PermissionCacheEpoch>();

    // Bridge the cookie principal into Blazor's auth state.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, PersistentCookieAuthenticationStateProvider>();

    // HttpClient to call API (server-to-server). Configurable so a deployed Web
    // can point somewhere other than the local development API.
    var apiBaseAddress = builder.Configuration["Api:BaseAddress"] ?? "https://localhost:5003/";
    builder.Services.AddSingleton<ApiLinkState>();
    builder.Services.AddTransient<ApiLinkHandler>();
    var apiClientBuilder = builder.Services.AddHttpClient("UpzmgApi", client =>
    {
        client.BaseAddress = new Uri(apiBaseAddress);
        // ApiLinkHandler sets the limit of each call; this one must not end the long ones first.
        client.Timeout = Timeout.InfiniteTimeSpan;
    }).AddHttpMessageHandler<ApiLinkHandler>();

    // Local development uses an untrusted dev cert by default.
    if (builder.Environment.IsDevelopment())
    {
        apiClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    builder.Services.AddMemoryCache(); // for caching JWT per session/user

    startupLogger.LogInformation("Web secret validation completed successfully.");

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // An address that matches nothing renders the app's own "no encontrada" page with its 404
    // status, instead of the browser's empty error page.
    app.UseStatusCodePagesWithReExecute("/no-encontrada", createScopeForStatusCodePages: true);

    // Three headers that need no tuning against the deployment, and a cache rule for the
    // documents that carry student data. A Content-Security-Policy is deliberately NOT set
    // here: Blazor Server needs its WebSocket and MudBlazor injects styles at runtime, so a
    // policy written blind breaks the product. It has to be measured in report-only mode on
    // the real deployment first.
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "same-origin";
        headers["X-Frame-Options"] = "SAMEORIGIN";

        // Decided when the response starts, because authentication has run by then: an
        // authenticated page or document is personal and must not be served from the back
        // button after a sign-out. Static assets and the Blazor transport keep their caching.
        context.Response.OnStarting(() =>
        {
            if (context.User.Identity?.IsAuthenticated == true
                && !context.Request.Path.StartsWithSegments("/_framework")
                && !context.Request.Path.StartsWithSegments("/_blazor")
                && !context.Request.Path.StartsWithSegments("/BulkImportTemplates"))
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
            }

            return Task.CompletedTask;
        });

        await next();
    });

    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "BulkImportTemplates")),
        RequestPath = "/BulkImportTemplates"
    });

    app.UseRouting();
    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    // Blazor components (Interactive Server) own the migrated pages.
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // MVC still serves the login flow and not-yet-migrated modules.
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Logger.LogInformation("UPZMG Web starting.");
    app.Run();
}
catch (Exception ex)
{
    startupLogger.LogCritical(ex, "UPZMG Web startup aborted.");
    throw;
}

static ILoggerFactory CreateStartupLoggerFactory()
{
    return LoggerFactory.Create(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
    });
}

static void ValidateRequiredSecret(IConfiguration configuration, string key, int minLength, ILogger logger)
{
    logger.LogInformation("Validating required secret configuration.");

    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Missing configuration 'SecretKey'. Configure it via user secrets or environment variables.");

    if (value.StartsWith("CHANGE_", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"Configuration 'SecretKey' still uses a placeholder value. Configure a real secret via user secrets or environment variables.");

    if (value.Length < minLength)
        throw new InvalidOperationException($"Configuration 'SecretKey' must be at least {minLength} characters long.");

    logger.LogInformation("Required secret configuration 'SecretKey' loaded successfully.");
}