using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;
using UPZMG.Implementations;
using UPZMG.Web.Services;

namespace UPZMG.Web.Controllers;

public class AccountController(
    ISecurityService securityService,
    ISecurityEventLogger securityLogger,
    ApiClient apiClient,
    IConfiguration configuration) : Controller
{
    private readonly ISecurityService _securityService = securityService;
    private readonly ISecurityEventLogger _securityLogger = securityLogger;
    private readonly ApiClient _apiClient = apiClient;
    private readonly IConfiguration _configuration = configuration;

    // The login page (GET) is now a Blazor component at /login. This controller
    // only handles the cookie sign-in POST (Blazor can't sign a cookie mid-circuit)
    // and redirects back to /login?error=1 on failure.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        try{
            // If the user is already authenticated, redirect to the dashboard
            if (User.Identity?.IsAuthenticated == true)
                return AfterSignIn(model.ReturnUrl);

            // If the user is not authenticated, proceed with login logic
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity();
            var props = new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            };

            var email = model.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
            {
                _securityLogger.LogLoginRejected(email, "Missing credentials");
                model.ErrorMessage = "Email y contraseña son requeridos.";
                return BackToLogin("1", model.ReturnUrl);
            }

            var sharedSecret = _configuration["InternalAuth:WebSharedSecret"]
                ?? throw new InvalidOperationException("Missing internal shared secret configuration.");

            // 1) Resolve and validate user through the API layer
            var request = new AccountWebLoginRequest
            {
                Email = email,
                Password = model.Password,
                SharedSecret = sharedSecret
            };
            var authenticatedUser = await _securityService.AuthenticateUserAsync(request);

            if (authenticatedUser is { Throttled: true })
            {
                _securityLogger.LogLoginRejected(email, "Sign-in temporarily blocked after repeated failures");
                var minutes = Math.Max(1, (int)Math.Ceiling(authenticatedUser.RetryAfterSeconds / 60.0));
                return BackToLogin($"espera&minutos={minutes}", model.ReturnUrl);
            }

            if (authenticatedUser is { Unavailable: true })
            {
                _securityLogger.LogLoginRejected(email, "The API did not answer the sign-in or refused this Web");
                return BackToLogin("servicio", model.ReturnUrl);
            }

            if (authenticatedUser is null || authenticatedUser.UserId == Guid.Empty || string.IsNullOrWhiteSpace(authenticatedUser.RoleName))
            {
                _securityLogger.LogLoginRejected(email, "User not found or inactive");
                model.ErrorMessage = "Credenciales inválidas.";
                return BackToLogin("1", model.ReturnUrl);
            }

            var claimEmail = authenticatedUser.Email?.Trim() ?? string.Empty;
            var claimRole = authenticatedUser.RoleName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(claimEmail) || string.IsNullOrWhiteSpace(claimRole))
            {
                _securityLogger.LogLoginRejected(email, "Authenticated user returned invalid claim values");
                model.ErrorMessage = "Credenciales inválidas.";
                return BackToLogin("1", model.ReturnUrl);
            }

            // 2) Create cookie claims from API-authenticated user
            claims.Add(new Claim(ClaimTypes.NameIdentifier, authenticatedUser.UserId.ToString()));
            claims.Add(new Claim(ClaimTypes.Email, claimEmail));
            claims.Add(new Claim(ClaimTypes.Role, claimRole));

            // Only when the account really has a name: the shell falls back to the address
            // rather than inventing one from it.
            if (!string.IsNullOrWhiteSpace(authenticatedUser.UserName))
                claims.Add(new Claim(ClaimTypes.Name, authenticatedUser.UserName.Trim()));

            identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    props);
            _securityLogger.LogLoginSucceeded(
                claimEmail,
                new Role { Name = claimRole });
        }
        catch (Exception ex)
        {
            _securityLogger.LogLoginRejected(model.Email ?? string.Empty, $"Exception during login: {ex.Message}");
            model.ErrorMessage = "Ocurrió un error durante el inicio de sesión. Por favor, inténtelo de nuevo.";
            return BackToLogin("servicio", model.ReturnUrl);
        }
        return AfterSignIn(model.ReturnUrl);
    }

    // Only an address of this site, and never one of the two pages that make no sense to come back to.
    private IActionResult AfterSignIn(string? returnUrl) =>
        LocalRedirect(Url.IsLocalUrl(returnUrl)
            && !returnUrl!.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/no-encontrada", StringComparison.OrdinalIgnoreCase)
                ? returnUrl
                : "/dashboard");

    private RedirectResult BackToLogin(string error, string? returnUrl) =>
        Redirect(Url.IsLocalUrl(returnUrl)
            ? $"/login?error={error}&ReturnUrl={Uri.EscapeDataString(returnUrl!)}"
            : $"/login?error={error}");

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        //handle logout, sign out of cookie auth, and log the event
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null;
        var email = User.FindFirstValue(ClaimTypes.Email);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _securityLogger.LogLogout(userId, email);
        return LocalRedirect("/login");
    }

    // Where the shell sends an account the API reports as suspended. Its cookie is still valid, so
    // without this the person stays signed in looking at screens whose every call is refused. It
    // asks the API again: being linked here must not be enough to sign anybody out.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Suspended()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId) || !await _apiClient.IsSuspendedAsync(userId, fresh: true))
            return LocalRedirect("/dashboard");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _securityLogger.LogLogout(userId, User.FindFirstValue(ClaimTypes.Email));
        return LocalRedirect("/login?error=baja");
    }

    // Authorization is enforced by Blazor's AuthorizeRouteView; if the cookie
    // middleware ever hits this path, send the user somewhere valid.
    [AllowAnonymous]
    // The authorization middleware stops a page the account may not open and lands here. It
    // used to send everybody to the dashboard without a word, so a link somebody did not have
    // the permission for looked broken instead of refused.
    public IActionResult AccessDenied() => LocalRedirect("/sin-permiso");

    // Heartbeat hit by client-side JS only while the user is active; the request
    // itself is what triggers the cookie middleware's sliding-expiration renewal.
    [Authorize]
    [HttpPost("/account/keepalive")]
    [ValidateAntiForgeryToken]
    public IActionResult KeepAlive() => NoContent();
}
