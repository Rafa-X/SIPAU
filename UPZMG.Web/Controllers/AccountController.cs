using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;
using UPZMG.Web.Services;

namespace UPZMG.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDBContext _db;
    private readonly ISecurityEventLogger _securityLogger;

    public AccountController(AppDBContext db, ISecurityEventLogger securityLogger)
    {
        _db = db;
        _securityLogger = securityLogger;
    }

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
        //handle login form submission, validate credentials, and sign in with cookie auth
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

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
            model.Password = null;
            return View(model);
        }

        // 1) Find active user
        var user = await _db.SystemUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email && x.IsActive);

        if (user is null)
        {
            _securityLogger.LogLoginRejected(email, "User not found or inactive");
            model.ErrorMessage = "Credenciales inválidas.";
            model.Password = null;
            return View(model);
        }

        // 2) Verify password hash
        var hasher = new PasswordHasher<SystemUser>();
        var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            _securityLogger.LogLoginRejected(email, "Invalid password");
            model.ErrorMessage = "Credenciales inválidas.";
            model.Password = null;
            return View(model);
        }

        // 3) Load roles
        var roles = await (
            from ur in _db.UserRoles.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == user.Id
            select r.Name
        ).ToListAsync();

        // 4) Create cookie claims
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                props);
        _securityLogger.LogLoginSucceeded(user.Email, roles);
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

    public IActionResult AccessDenied() => View();
}
