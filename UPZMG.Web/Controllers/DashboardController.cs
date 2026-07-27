using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPZMG.Web.Models;

namespace UPZMG.Web.Controllers;

[Authorize]
public class DashboardController : SigieBaseController
{
    public IActionResult Index()
    {
        SetUserViewData();

        // Build demo ViewModel (to be replaced with real DB/API queries later)
        var model = new DashboardViewModel();
        PopulateDashboardModel(model);

        return View(model);
    }

    private static void PopulateDashboardModel(DashboardViewModel model)
    {
        // Period
        model.ActivePeriod = "Ciclo 2025-2026 (Cuatrimestre 1)";
        model.IsPeriodActive = true;

        // Stats
        model.TotalStudents = 1245;
        model.NewEnrollments = 320;
        model.ApprovalRate = 87.5m;
        model.FailureRate = 12.5m;
        model.TotalRevenue = 15_500_000m;
        model.RevenuePeriod = "Periodo actual";
        model.TotalTeachers = 85;
        model.TeachersWithPostgrad = 62;

        // Summary
        model.CompletedAreas = 4;
        model.TotalAreas = 6;

        // Alerts
        model.Alerts = new List<AlertBannerModel>
        {
            new() { Message = "Área de Vinculación tiene captura pendiente desde hace 2 días", Type = "warning", Priority = "Media" },
            new() { Message = "Reporte 911 debe enviarse en 15 días", Type = "info", Priority = "Alta" },
        };

        // capture progress per area
        model.AreaProgress = new List<CaptureAreaProgress>
        {
            new() { AreaName = "Servicios Escolares", ResponsiblePerson = "Lic. Ana Martínez", Percentage = 100, Status = "Completo" },
            new() { AreaName = "Recursos Humanos", ResponsiblePerson = "Lic. Carlos López", Percentage = 100, Status = "Completo" },
            new() { AreaName = "Vinculación", ResponsiblePerson = "Mtro. Pedro Sánchez", Percentage = 68, Status = "En progreso" },
            new() { AreaName = "Finanzas", ResponsiblePerson = "C.P. María García", Percentage = 100, Status = "Completo" },
            new() { AreaName = "Infraestructura y TIC", ResponsiblePerson = "Ing. Roberto Díaz", Percentage = 45, Status = "En progreso" },
            new() { AreaName = "Investigación", ResponsiblePerson = "Dr. Laura Hernández", Percentage = 100, Status = "Completo" },
        };
    }
}
