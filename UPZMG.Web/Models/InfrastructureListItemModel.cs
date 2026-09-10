namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the captured-infrastructure listing.
/// </summary>
public class InfrastructureListItemModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UseType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime CapturedOn { get; set; }
}
