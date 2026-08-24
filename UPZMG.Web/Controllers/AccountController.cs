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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {   
        //show login page if not authenticated, otherwise redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {   
        try{
            // If the user is already authenticated, redirect to the dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

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
                return View(model);
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
                return View(model);
            }

            var claimEmail = authenticatedUser.Email?.Trim() ?? string.Empty;
            var claimRole = authenticatedUser.RoleName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(claimEmail) || string.IsNullOrWhiteSpace(claimRole))
            {
                _securityLogger.LogLoginRejected(email, "Authenticated user returned invalid claim values");
                model.ErrorMessage = "Credenciales inválidas.";
                return View(model);
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
            return View(model);
        }
        return RedirectToAction("Index", "Dashboard");
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
        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
