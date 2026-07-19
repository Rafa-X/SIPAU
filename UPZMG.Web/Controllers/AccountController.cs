using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;
using UPZMG.Web.Services;
using UPZMG.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace UPZMG.Web.Controllers;

public class AccountController(AppDBContext _db, ISecurityEventLogger _securityLogger) : Controller
{

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

            // 1) Find active user
            var user = await _db.GetSystemUserAsync(email);

            if (user is null || user.IsActive == false || user.Role is null || string.IsNullOrWhiteSpace(user.Password))
            {
                _securityLogger.LogLoginRejected(email, "User not found or inactive");
                model.ErrorMessage = "Credenciales inválidas.";
                return View(model);
            }

            // 2) Verify password hash
            var hasher = new PasswordHasher<SystemUser>();
            var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                _securityLogger.LogLoginRejected(email, "Invalid password");
                model.ErrorMessage = "Credenciales inválidas.";
                return View(model);
            }

            // 3) Create cookie claims
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));

            identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    props);
                _securityLogger.LogLoginSucceeded(user.Email, user.Role);
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
