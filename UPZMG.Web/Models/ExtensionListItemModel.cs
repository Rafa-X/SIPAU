namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the captured-extension/linkage listing.
/// </summary>
public class ExtensionListItemModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime CapturedOn { get; set; }
}
