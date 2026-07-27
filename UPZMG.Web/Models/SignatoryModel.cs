namespace UPZMG.Web.Models;

/// <summary>
/// Represents a signatory of board minutes.
/// </summary>
public class SignatoryModel
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;

    /// <summary> "Firmado" or "Pendiente" </summary>
    public string Status { get; set; } = "Pendiente";

    public DateTime? SignedDate { get; set; }
}
