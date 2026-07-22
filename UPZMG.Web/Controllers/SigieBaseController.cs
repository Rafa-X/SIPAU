using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UPZMG.Web.Controllers;

/// <summary>
/// Base controller for all authenticated SIGIE pages.
/// Provides shared user-info extraction used by the sidebar partial.
/// </summary>
[Authorize]
public abstract class SigieBaseController : Controller
{
    /// <summary>
    /// Extracts display name and initials from the authenticated user's email claim
    /// and populates ViewData keys consumed by _SidebarPartial.
    /// </summary>
    protected (string DisplayName, string Initials) SetUserViewData()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var roleName = User.FindFirstValue(ClaimTypes.Role) ?? "—";
        var emailParts = email.Split('@')[0]
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        var displayName = string.Join(" ", emailParts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..]));
        var initials = string.Join("", emailParts.Select(p =>
            char.ToUpperInvariant(p[0])));
        if (initials.Length > 2) initials = initials[..2];

        ViewData["UserName"] = displayName;
        ViewData["UserRole"] = roleName;
        ViewData["UserInitials"] = initials;

        return (displayName, initials);
    }
}
