namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the board minutes listing.
/// </summary>
public class MinuteListItemModel
{
    public int Id { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    /// <summary> "Pendiente de firmas", "En revisión gobierno", or "Firmada" </summary>
    public string Status { get; set; } = "Pendiente de firmas";
}
