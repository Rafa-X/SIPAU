namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the board minutes landing page.
/// </summary>
public class MinutesIndexViewModel
{
    public List<MinuteListItemModel> Minutes { get; set; } = new();

    // User info (from claims)
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;
}
