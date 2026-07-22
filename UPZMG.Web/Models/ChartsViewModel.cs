namespace UPZMG.Web.Models;

/// <summary>
/// Mock data for the 4 charts in the Charts module.
/// Lists are serialized to JSON and consumed by Chart.js in the view.
/// </summary>
public class ChartsViewModel
{
    // (1) Historical enrollment + projection (dashed line for future periods)
    public List<string> EnrollmentPeriods { get; set; } = new();
    public List<int?> HistoricalEnrollment { get; set; } = new();
    public List<int?> ProjectedEnrollment { get; set; } = new();

    // (2) Student distribution by career
    public List<string> Careers { get; set; } = new();
    public List<int> StudentsByCareer { get; set; } = new();

    // (3) Approval vs failure rate by period
    public List<string> AchievementPeriods { get; set; } = new();
    public List<decimal> ApprovalRate { get; set; } = new();
    public List<decimal> FailureRate { get; set; } = new();

    // (4) Scholarships assigned by type / funding source
    public List<string> ScholarshipTypes { get; set; } = new();
    public List<int> ScholarshipsAssigned { get; set; } = new();

    // User info (from claims)
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;
}
