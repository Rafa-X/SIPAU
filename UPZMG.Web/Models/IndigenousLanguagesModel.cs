namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the captured-indigenous-languages listing.
/// </summary>
public class IndigenousLanguagesModel
{
    public int Id { get; set; }
    public string Language { get; set; } = string.Empty;
}