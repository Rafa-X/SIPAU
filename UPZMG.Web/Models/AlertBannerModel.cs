namespace UPZMG.Web.Models;

/// <summary>
/// Model for reusable alert banner partial view.
/// Supports info, warning, and success types with priority levels.
/// </summary>
public class AlertBannerModel
{
    public string Mensaje { get; set; } = string.Empty;

    /// <summary> "info", "warning", or "success" </summary>
    public string Tipo { get; set; } = "info";

    /// <summary> "Alta", "Media", or "Baja" </summary>
    public string Prioridad { get; set; } = "Media";
}
