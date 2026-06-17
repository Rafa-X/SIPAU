namespace UPZMG.Web.Models;

/// <summary>
/// Represents the capture progress of a single area for the dashboard.
/// </summary>
public class CaptureAreaProgress
{
    public string NombreArea { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public int Porcentaje { get; set; }

    /// <summary> "Completo", "En progreso", or "Pendiente" </summary>
    public string Estado { get; set; } = "Pendiente";
}
