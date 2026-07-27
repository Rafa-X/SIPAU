namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for a single board minute's detail page (signatories and their status).
/// </summary>
public class MinuteDetailViewModel
{
    public int Id { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Pendiente de firmas";
    public List<SignatoryModel> Signatories { get; set; } = new();

    // User info (from claims)
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;
}
