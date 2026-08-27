using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace UPZMG.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    /// <summary>
    /// Home redirects to the Dashboard (the main landing page after login).
    /// </summary>
    public IActionResult Index()
    {
        return LocalRedirect("/dashboard");
    }
}
