namespace UPZMG.Web.Models;

/// <summary>
/// Model for the reusable alert banner partial view.
/// Supports info, warning, and success types with priority levels.
/// </summary>
public class AlertBannerModel
{
    public string Message { get; set; } = string.Empty;

    /// <summary> "info", "warning", or "success" </summary>
    public string Type { get; set; } = "info";

    /// <summary> "Alta", "Media", or "Baja" (display-only labels) </summary>
    public string Priority { get; set; } = "Media";
}
