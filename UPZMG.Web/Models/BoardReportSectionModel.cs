namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single section in the board report's table of contents.
/// </summary>
public class BoardReportSectionModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "📄";
}
