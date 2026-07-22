namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the board report listing.
/// </summary>
public class BoardReportListItemModel
{
    public int Id { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }

    /// <summary> "Borrador", "Generado", or "Enviado" </summary>
    public string Status { get; set; } = "Borrador";

    public int PageCount { get; set; }
}
