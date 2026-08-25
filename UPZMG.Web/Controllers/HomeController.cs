using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UPZMG.Web.Models;

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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
