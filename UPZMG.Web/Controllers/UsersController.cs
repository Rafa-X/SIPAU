using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UPZMG.Web.Models.Users;

namespace UPZMG.Web.Controllers;

/// <summary>
/// User management module (admin only). Lists system users and lets an admin
/// create or edit them, assigning exactly one role per user. All writes are mocked.
/// </summary>
[Authorize(Roles = "Admin")]
public class UsersController : SigieBaseController
{
    // Mock in-memory catalog (to be replaced with real DB/API queries later)
    private static readonly List<UserListItemModel> MockUsers = new()
    {
        new() { Id = 1, FullName = "Francisco Subdirección", Email = "francisco.planeacion@upzmg.edu.mx", RoleName = "Admin", IsActive = true, LastLogin = DateTime.Now.AddHours(-3) },
        new() { Id = 2, FullName = "Karla Asistente", Email = "karla.asistente@upzmg.edu.mx", RoleName = "Planeación", IsActive = true, LastLogin = DateTime.Now.AddDays(-1) },
        new() { Id = 3, FullName = "Ana Martínez", Email = "ana.martinez@upzmg.edu.mx", RoleName = "Control Escolar", IsActive = true, LastLogin = DateTime.Now.AddDays(-2) },
        new() { Id = 4, FullName = "Carlos López", Email = "carlos.lopez@upzmg.edu.mx", RoleName = "Recursos Humanos", IsActive = false, LastLogin = DateTime.Now.AddDays(-30) },
    };

    public IActionResult Index()
    {
        SetUserViewData();

        if (TempData["UsersStatus"] is string statusMessage)
            ViewData["UsersStatus"] = statusMessage;

        return View(new UsersIndexViewModel { Users = MockUsers });
    }

    [HttpGet]
    public IActionResult Create()
    {
        SetUserViewData();
        return View(new UserFormViewModel { Roles = GetMockRoles() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(UserFormViewModel model)
    {
        TempData["UsersStatus"] = $"Usuario '{model.Email}' creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        SetUserViewData();

        var user = MockUsers.FirstOrDefault(u => u.Id == id) ?? MockUsers[0];

        var model = new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleName,
            IsActive = user.IsActive,
            Roles = GetMockRoles()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(UserFormViewModel model)
    {
        TempData["UsersStatus"] = $"Usuario '{model.Email}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private static List<SelectListItem> GetMockRoles() => new()
    {
        new SelectListItem("Admin", "Admin"),
        new SelectListItem("Rectoría", "Rectoría"),
        new SelectListItem("Planeación", "Planeación"),
        new SelectListItem("Control Escolar", "Control Escolar"),
        new SelectListItem("Recursos Humanos", "Recursos Humanos"),
        new SelectListItem("Finanzas", "Finanzas"),
    };
}
