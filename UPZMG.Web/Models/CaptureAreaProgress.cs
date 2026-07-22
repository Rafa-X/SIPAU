namespace UPZMG.Web.Models;

/// <summary>
/// Represents the capture progress of a single area for the dashboard.
/// </summary>
public class CaptureAreaProgress
{
    public string AreaName { get; set; } = string.Empty;
    public string ResponsiblePerson { get; set; } = string.Empty;
    public int Percentage { get; set; }

    /// <summary> "Completo", "En progreso", or "Pendiente" (display-only labels) </summary>
    public string Status { get; set; } = "Pendiente";
}
