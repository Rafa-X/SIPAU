namespace UPZMG.Web.Models;

/// <summary>
/// Represents a data capture area within the DataCapture module.
/// </summary>
public class CaptureAreaModel
{
    public int Id { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string ResponsiblePerson { get; set; } = string.Empty;
    public int Percentage { get; set; }

    /// <summary> "Completo", "En progreso", or "Pendiente" </summary>
    public string Status { get; set; } = "Pendiente";

    public DateTime LastUpdated { get; set; }
    public string Icon { get; set; } = "📋";

    /// <summary> Whether this area has a capture form implemented in the current sprint. </summary>
    public bool IsCaptureAvailable { get; set; }

    /// <summary>
    /// The DataCapture action this area's "Capturar" button routes to
    /// (e.g. "Students", "Teachers"). Empty when no form exists yet.
    /// </summary>
    public string ActionName { get; set; } = string.Empty;
}
