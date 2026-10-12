using System.ComponentModel.DataAnnotations;
using UPZMG.Persistence.Models;

namespace UPZMG.Implementations.Reports;

/// <summary>A saved capture as listed for its owner, without answers.</summary>
public sealed record ReportListItem(
    Guid Id,
    string ReportType,
    string Version,
    ReportStatus Status,
    DateTime DateCreated,
    DateTime LastModified);

/// <summary>A saved capture with its answers, to resume or review it.</summary>
/// <param name="RowVersion">
/// The version this capture had when it was read, as the database stamps it. The form sends it
/// back on save so a write that was decided against older content is refused instead of
/// overwriting what somebody else stored meanwhile.
/// </param>
public sealed record ReportDetail(
    Guid Id,
    string ReportType,
    string Version,
    ReportStatus Status,
    DateTime DateCreated,
    DateTime LastModified,
    Dictionary<string, string?> Answers,
    string? RowVersion = null);

public sealed class SaveReport
{
    public required Guid RequesterKey { get; set; }

    /// <summary>Capture to update; null on its first save.</summary>
    public Guid? ReportId { get; set; }

    [MaxLength(40)]
    public required string ReportType { get; set; }

    [MaxLength(20)]
    public required string Version { get; set; }

    /// <summary>draft keeps it editable; submitted freezes it.</summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Version the form was opened with, from <see cref="ReportDetail.RowVersion"/>. Null on a
    /// first save, when there is nothing to overwrite.
    /// </summary>
    public string? RowVersion { get; set; }

    public Dictionary<string, string?> Answers { get; set; } = new();
}

/// <param name="RowVersion">The version the capture now has, for the next save from this form.</param>
public sealed record ReportSaved(Guid Id, ReportStatus Status, DateTime LastModified, string? RowVersion = null);

public enum SaveReportOutcome
{
    Created,
    Updated,
    NotFound,

    /// <summary>Already submitted: an official document is not edited in place.</summary>
    Locked,

    /// <summary>Somebody else saved it after this form read it.</summary>
    Stale,

    Incomplete,
}

public sealed record SaveReportResult(
    SaveReportOutcome Outcome,
    ReportSaved? Report = null,
    IReadOnlyList<string>? Problems = null);

/// <summary>A template as the author edits it: plain JSON text.</summary>
public sealed class SaveReportTemplate
{
    public required Guid RequesterKey { get; set; }

    /// <summary>File being edited ("Carrera911_9A_2024-2025"); null when creating one.</summary>
    public string? ReplacingFile { get; set; }

    public required string Json { get; set; }
}

public sealed class DeleteReportTemplate
{
    public required Guid RequesterKey { get; set; }

    [MaxLength(40)]
    public required string ReportType { get; set; }

    [MaxLength(20)]
    public required string Version { get; set; }
}

/// <summary>Outcome of writing or removing a template; the problems are the reason it was refused.</summary>
public sealed record TemplateSaveResult(bool Saved, string? FileKey = null, IReadOnlyList<string>? Problems = null)
{
    public static TemplateSaveResult Refused(params string[] problems) => new(false, null, problems);
}

/// <summary>A template file as the editor loads it. Wrapped in an object so it
/// travels as JSON instead of as a bare text/plain body.</summary>
public sealed record ReportTemplateSource(string ReportType, string Version, string Json);
