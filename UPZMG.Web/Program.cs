using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using UPZMG.Web.Components;
using UPZMG.Web.Services;
using UPZMG.Persistence;

Console.Title = "UPZMG.Web";
var builder = WebApplication.CreateBuilder(args);
using var startupLoggerFactory = CreateStartupLoggerFactory();
var startupLogger = startupLoggerFactory.CreateLogger("Startup");

try
{
    startupLogger.LogInformation("Validating Web startup secrets.");

    ValidateRequiredSecret(builder.Configuration, "InternalAuth:WebSharedSecret", 32, startupLogger);

    builder.Services.AddControllersWithViews();

    // Blazor Web App (Interactive Server) + MudBlazor, coexisting with MVC.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices();

    builder.Services.AddDbContext<AppDBContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

    // Cookie Auth
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "UPZMG.Auth";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(3);
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    builder.Services.AddScoped<ApiTokenService>();
    builder.Services.AddScoped<ApiClient>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddSingleton<ISecurityEventLogger, SecurityLogger>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddSingleton<IUsersService, UsersService>();
    builder.Services.AddScoped<IDataCaptureService, DataCaptureService>();
    builder.Services.AddScoped<IMinutesService, MinutesService>();
    builder.Services.AddScoped<IBoardReportService, BoardReportService>();
    builder.Services.AddSingleton<ISettingsService, SettingsService>();
    builder.Services.AddScoped<IReportsService, ReportsService>();
    builder.Services.AddScoped<ISecurityService, SecurityService>();
    builder.Services.AddSingleton<IIntelligenceService, IntelligenceService>();

    builder.Services.AddAuthorization();

    // Bridge the cookie principal into Blazor's auth state.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, PersistentCookieAuthenticationStateProvider>();

    // HttpClient to call API (server-to-server)
    var apiClientBuilder = builder.Services.AddHttpClient("UpzmgApi", client =>
    {
        client.BaseAddress = new Uri("https://localhost:5003/");
    });

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
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

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