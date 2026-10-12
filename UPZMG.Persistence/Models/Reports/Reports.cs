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

    /// <summary>
    /// The consolidated document as it was submitted, for the board report only. Everything it
    /// consolidates — enrolment, risk, scholarships, finance — goes on changing afterwards, so
    /// without this the figures of a document that was already sent changed with them.
    /// </summary>
    public string? SnapshotJson { get; set; }
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