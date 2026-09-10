using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

/// <summary>
/// Supplies the dashboard view model. Currently returns demo data;
/// swap the body for real DB/API queries later without touching the UI.
/// </summary>
public interface IDashboardService
{
    DashboardViewModel GetDashboard();
}

public sealed class DashboardService : IDashboardService
{
    private readonly ISettingsService _settings;

    public DashboardService(ISettingsService settings) => _settings = settings;

    public DashboardViewModel GetDashboard()
    {
        // The active period has exactly one owner: the settings module. Hard-coding
        // it here as well meant the dashboard could disagree with Configuración
        // about which cycle the figures belong to.
        var period = _settings.ActivePeriod;

        var model = new DashboardViewModel
        {
            ActivePeriod = period is null
                ? "Sin periodo activo"
                : $"Ciclo {period.SchoolCycle} · {period.Code}",
            IsPeriodActive = period is not null,

            TotalStudents = 1245,
            NewEnrollments = 320,
            ApprovalRate = 87.5m,
            FailureRate = 12.5m,
            TotalRevenue = 15_500_000m,
            RevenuePeriod = "Periodo actual",
            TotalTeachers = 85,
            TeachersWithPostgrad = 62,

            CompletedAreas = 4,
            TotalAreas = 6,

            Alerts = new List<AlertBannerModel>
            {
                new() { Message = "Área de Vinculación tiene captura pendiente desde hace 2 días", Type = "warning", Priority = "Media" },
                new() { Message = "Reporte 911 debe enviarse en 15 días", Type = "info", Priority = "Alta" },
            },

            AreaProgress = new List<DataCaptureAreaProgress>
            {
                new() { AreaName = "Servicios Escolares", ResponsiblePerson = "Lic. Ana Martínez", Percentage = 100, Status = "Completo" },
                new() { AreaName = "Recursos Humanos", ResponsiblePerson = "Lic. Carlos López", Percentage = 100, Status = "Completo" },
                new() { AreaName = "Vinculación", ResponsiblePerson = "Mtro. Pedro Sánchez", Percentage = 68, Status = "En progreso" },
                new() { AreaName = "Finanzas", ResponsiblePerson = "C.P. María García", Percentage = 100, Status = "Completo" },
                new() { AreaName = "Infraestructura y TIC", ResponsiblePerson = "Ing. Roberto Díaz", Percentage = 45, Status = "En progreso" },
                new() { AreaName = "Investigación", ResponsiblePerson = "Dr. Laura Hernández", Percentage = 100, Status = "Completo" },
            },
        };

        return model;
    }
}
