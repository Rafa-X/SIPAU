using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPZMG.Web.Models;

namespace UPZMG.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public async Task<IActionResult> Index(DashboardViewModel? model = null)
    {
        model ??= new DashboardViewModel();

        // Extract user info from claims
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var roleName = User.FindFirstValue(ClaimTypes.Role) ?? "—";

        // Build user display info
        var emailParts = email.Split('@')[0]
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        var displayName = string.Join(" ", emailParts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..]));
        var initials = string.Join("", emailParts.Select(p =>
            char.ToUpperInvariant(p[0])));

        // Fill the same model instance with fresh data for the current request.
        PopulateDashboardModel(model);

        // Pass user info to layout's sidebar
        ViewData["UserName"] = displayName;
        ViewData["UserRole"] = roleName;
        ViewData["UserInitials"] = initials;

        return View(model);
    }

    private static void PopulateDashboardModel(DashboardViewModel model)
    {
        // Period
        model.PeriodoActivo = "Ciclo 2025-2026 (Cuatrimestre 1)";
        model.PeriodoEsActivo = true;

        // Stats
        model.TotalAlumnos = 1245;
        model.NuevosIngresos = 320;
        model.IndiceAprobacion = 87.5m;
        model.IndiceReprobacion = 12.5m;
        model.IngresosTotales = 15_500_000m;
        model.PeriodoIngresos = "Periodo actual";
        model.TotalDocentes = 85;
        model.DocentesConPosgrado = 62;

        // Summary
        model.AreasCompletadas = 4;
        model.TotalAreas = 6;

        // Alerts
        model.Alertas = new List<AlertBannerModel>
        {
            new() { Mensaje = "Área de Vinculación tiene captura pendiente desde hace 2 días", Tipo = "warning", Prioridad = "Media" },
            new() { Mensaje = "Reporte 911 debe enviarse en 15 días", Tipo = "info", Prioridad = "Alta" },
        };

        // Capture progress per area
        model.AreasProgreso = new List<CaptureAreaProgress>
        {
            new() { NombreArea = "Servicios Escolares", Responsable = "Lic. Ana Martínez", Porcentaje = 100, Estado = "Completo" },
            new() { NombreArea = "Recursos Humanos", Responsable = "Lic. Carlos López", Porcentaje = 100, Estado = "Completo" },
            new() { NombreArea = "Vinculación", Responsable = "Mtro. Pedro Sánchez", Porcentaje = 68, Estado = "En progreso" },
            new() { NombreArea = "Finanzas", Responsable = "C.P. María García", Porcentaje = 100, Estado = "Completo" },
            new() { NombreArea = "Infraestructura y TIC", Responsable = "Ing. Roberto Díaz", Porcentaje = 45, Estado = "En progreso" },
            new() { NombreArea = "Investigación", Responsable = "Dr. Laura Hernández", Porcentaje = 100, Estado = "Completo" },
        };
    }
}