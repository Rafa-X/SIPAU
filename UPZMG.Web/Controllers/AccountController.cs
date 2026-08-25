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

public class AccountController(DataService dataService, ISecurityEventLogger securityLogger, IConfiguration configuration) : Controller
{
    private readonly DataService _dataService = dataService;
    private readonly ISecurityEventLogger _securityLogger = securityLogger;
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
                return LocalRedirect("/dashboard");

            // If the user is not authenticated, proceed with login logic
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity();
            var props = new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false
            };

            var email = model.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
            {
                _securityLogger.LogLoginRejected(email, "Missing credentials");
                model.ErrorMessage = "Email y contraseña son requeridos.";
                return Redirect("/login?error=1");
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
            var authenticatedUser = await _dataService.AuthenticateUserAsync(request);

            if (authenticatedUser is null || authenticatedUser.UserId == Guid.Empty || string.IsNullOrWhiteSpace(authenticatedUser.RoleName))
            {
                _securityLogger.LogLoginRejected(email, "User not found or inactive");
                model.ErrorMessage = "Credenciales inválidas.";
                return Redirect("/login?error=1");
            }

            var claimEmail = authenticatedUser.Email?.Trim() ?? string.Empty;
            var claimRole = authenticatedUser.RoleName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(claimEmail) || string.IsNullOrWhiteSpace(claimRole))
            {
                _securityLogger.LogLoginRejected(email, "Authenticated user returned invalid claim values");
                model.ErrorMessage = "Credenciales inválidas.";
                return Redirect("/login?error=1");
            }

            // 2) Create cookie claims from API-authenticated user
            claims.Add(new Claim(ClaimTypes.NameIdentifier, authenticatedUser.UserId.ToString()));
            claims.Add(new Claim(ClaimTypes.Email, claimEmail));
            claims.Add(new Claim(ClaimTypes.Role, claimRole));

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
            return Redirect("/login?error=1");
        }
        return LocalRedirect("/dashboard");
    }

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

    // Authorization is enforced by Blazor's AuthorizeRouteView; if the cookie
    // middleware ever hits this path, send the user somewhere valid.
    [AllowAnonymous]
    public IActionResult AccessDenied() => LocalRedirect("/dashboard");
}
