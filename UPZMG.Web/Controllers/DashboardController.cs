using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPZMG.Web.Models;
using UPZMG.Web.Services;

namespace UPZMG.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAuthService _authService;

    public DashboardController(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        // Extract user info from claims
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();  // Get live roles from API
        //var roles = await _authService.GetCurrentRolesAsync(User);  // Get live roles from API

        // Build user display info
        var emailParts = email.Split('@')[0]
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        var displayName = string.Join(" ", emailParts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..]));
        var initials = string.Join("", emailParts.Select(p =>
            char.ToUpperInvariant(p[0])));

        // Build demo ViewModel (to be replaced with real DB queries later)
        var model = new DashboardViewModel
        {
            // Period
            PeriodoActivo = "Ciclo 2025-2026 (Cuatrimestre 1)",
            PeriodoEsActivo = true,

            // Stats
            TotalAlumnos = 1245,
            NuevosIngresos = 320,
            IndiceAprobacion = 87.5m,
            IndiceReprobacion = 12.5m,
            IngresosTotales = 15_500_000m,
            PeriodoIngresos = "Periodo actual",
            TotalDocentes = 85,
            DocentesConPosgrado = 62,

            // Summary
            AreasCompletadas = 4,
            TotalAreas = 6,

            // User
            NombreUsuario = displayName,
            InicialesUsuario = initials,

            // Alerts
            Alertas = new List<AlertBannerModel>
            {
                new() { Mensaje = "Área de Vinculación tiene captura pendiente desde hace 2 días", Tipo = "warning", Prioridad = "Media" },
                new() { Mensaje = "Reporte 911 debe enviarse en 15 días", Tipo = "info", Prioridad = "Alta" },
            },

            // Capture progress per area
            AreasProgreso = new List<CaptureAreaProgress>
            {
                new() { NombreArea = "Servicios Escolares", Responsable = "Lic. Ana Martínez", Porcentaje = 100, Estado = "Completo" },
                new() { NombreArea = "Recursos Humanos", Responsable = "Lic. Carlos López", Porcentaje = 100, Estado = "Completo" },
                new() { NombreArea = "Vinculación", Responsable = "Mtro. Pedro Sánchez", Porcentaje = 68, Estado = "En progreso" },
                new() { NombreArea = "Finanzas", Responsable = "C.P. María García", Porcentaje = 100, Estado = "Completo" },
                new() { NombreArea = "Infraestructura y TIC", Responsable = "Ing. Roberto Díaz", Porcentaje = 45, Estado = "En progreso" },
                new() { NombreArea = "Investigación", Responsable = "Dr. Laura Hernández", Porcentaje = 100, Estado = "Completo" },
            }
        };

        // Pass user info to layout's sidebar
        ViewData["UserName"] = model.NombreUsuario;
        ViewData["UserRole"] = model.RolUsuario;
        ViewData["UserInitials"] = model.InicialesUsuario;

        return View(model);
    }
}