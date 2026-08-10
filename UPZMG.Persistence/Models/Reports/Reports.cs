namespace UPZMG.Persistence.Models;

/// <summary>
/// Represents a report in the system, can be a draft or completed.
/// </summary>

public class Reports
{
    public Guid Id { get; set; }
    public Guid? CreatedBy { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string? TemplateVersion { get; set; }
    public string Status { get; set; } = ReportStatus.draft.ToString();
    public string AnswersJson { get; set; } = "{}";
    public DateTime DateCreated { get; set; }
    public DateTime LastModified { get; set; }
}

public enum ReportStatus
{
    draft,
    submitted,
    approved,
    rejected
}