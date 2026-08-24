using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UPZMG.Implementations;
using UPZMG.Web.Models.Users;
using UPZMG.Web.Services;
using UPZMG.Implementations.Users;
using System.Security.Claims;

namespace UPZMG.Web.Controllers;

/// <summary>
/// User management module (admin only). Lists system users and lets an admin
/// create or edit them, assigning exactly one role per user. All writes are mocked.
/// </summary>
[Authorize(Roles = "Admin")]
public class UsersController(DataService dataService): SigieBaseController
{
    public async Task<IActionResult> Index()
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var requesterKey))
            return Unauthorized();

        SetUserViewData();
        if (TempData["UsersStatus"] is string statusMessage)
            ViewData["UsersStatus"] = statusMessage;

        var request = new GetAllUsers
        {
            RequesterKey = requesterKey,
        };
        var users = await dataService.GetAllUsers(request); // Replace with actual method to get users from DB/API

        return View(new UsersIndexViewModel { Users = users });
    }

    [HttpGet]
    public async Task<IActionResult> CreateTemplate()
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var requesterKey))
            return Unauthorized();
        //SetUserViewData();

        var request = new GetAllRoles
        {
            RequesterKey = requesterKey,
        };
        var roles = await dataService.GetAllRoles(request);
        return View("UserTemplate", new UserFormViewModel
        {
            Roles = [.. roles.Select(role => new SelectListItem(role.Name, role.Id.ToString()))]
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAction(UserFormViewModel model)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var requesterKey))
            return Unauthorized();

        var createRequest = new CreateUser
        {
            RequesterKey = requesterKey,
            UserName = model.UserName,
            Email = model.Email,
            RoleId = model.RoleId,
            IsActive = model.IsActive,
            Password = model.Password
        };
        var result = await dataService.CreateUser(createRequest);

        TempData["UsersStatus"] = $"Usuario '{model.Email}' creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditTemplate(Guid userId)
    {
        //SetUserViewData();
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var requesterKey))
            return Unauthorized();

        var request = new GetUserById
        {
            RequesterKey = requesterKey,
            UserId = userId
        };
        var user = await dataService.GetUserById(request);
        var roles = await dataService.GetAllRoles(new GetAllRoles
        {
            RequesterKey = requesterKey,
        });

        var model = new UserFormViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            RoleId = user.Role?.Id.ToString() ?? string.Empty,
            IsActive = user.IsActive,
            Roles = [.. roles.Select(role => new SelectListItem(role.Name, role.Id.ToString()))]
        };

        return View("UserTemplate", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAction(UserFormViewModel model)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var requesterKey))
            return Unauthorized();

        var request = new EditUser
        {
            RequesterKey = requesterKey,
            UserId = model.Id,
            UserName = model.UserName,
            Password = model.Password,
            RoleId = model.RoleId,
            IsActive = model.IsActive
        };

        var result = await dataService.EditUser(request);
        
        TempData["UsersStatus"] = $"Usuario '{model.Email}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
