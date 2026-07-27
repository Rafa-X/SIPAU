namespace UPZMG.Web.Models;

/// <summary>
/// Represents the data displayed on the SIGIE dashboard.
/// Feeds stats, alerts, and capture progress sections.
/// </summary>
public class DashboardViewModel
{
    // Period header
    public string ActivePeriod { get; set; } = string.Empty;
    public bool IsPeriodActive { get; set; }

    // Stat cards
    public int TotalStudents { get; set; }
    public int NewEnrollments { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal FailureRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public string RevenuePeriod { get; set; } = string.Empty;
    public int TotalTeachers { get; set; }
    public int TeachersWithPostgrad { get; set; }

    // Summary
    public int CompletedAreas { get; set; }
    public int TotalAreas { get; set; }

    // Alerts
    public List<AlertBannerModel> Alerts { get; set; } = new();

    // Capture progress
    public List<CaptureAreaProgress> AreaProgress { get; set; } = new();
}
