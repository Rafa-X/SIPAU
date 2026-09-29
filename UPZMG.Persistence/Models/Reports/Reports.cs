using System.ComponentModel.DataAnnotations;
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

    /// <summary>
    /// Row version. Two areas can have the same capture open; whoever saves second writes against
    /// the version they read, so a save can no longer overwrite a submission it never saw.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

public enum ReportStatus
{
    draft,
    submitted,
    approved,
    rejected
}