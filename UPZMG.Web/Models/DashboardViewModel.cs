namespace UPZMG.Web.Models;

/// <summary>
/// Represents the data displayed on the SIGIE dashboard.
/// Feeds stats, alerts, and capture progress sections.
/// </summary>
public class DashboardViewModel
{
    // Period header
    public string PeriodoActivo { get; set; } = string.Empty;
    public bool PeriodoEsActivo { get; set; }

    // Stat cards
    public int TotalAlumnos { get; set; }
    public int NuevosIngresos { get; set; }
    public decimal IndiceAprobacion { get; set; }
    public decimal IndiceReprobacion { get; set; }
    public decimal IngresosTotales { get; set; }
    public string PeriodoIngresos { get; set; } = string.Empty;
    public int TotalDocentes { get; set; }
    public int DocentesConPosgrado { get; set; }

    // Summary
    public int AreasCompletadas { get; set; }
    public int TotalAreas { get; set; }

    // Alerts
    public List<AlertBannerModel> Alertas { get; set; } = new();

    // Capture progress
    public List<CaptureAreaProgress> AreasProgreso { get; set; } = new();

    // User info (from claims)
    public string NombreUsuario { get; set; } = string.Empty;
    public string RolUsuario { get; set; } = string.Empty;
    public string InicialesUsuario { get; set; } = string.Empty;
}
