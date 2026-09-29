namespace UPZMG.Implementations.Institution;

/// <summary>
/// The institution's master data as the university itself reported it in the official
/// 911.10 format. There is no separate table for it on purpose: the figure the Junta and
/// the SEP read must be the one that was captured and sent, not a second copy that can drift.
/// </summary>
/// <param name="SourceCycle">Cycle of the capture these values come from; null when none exists.</param>
/// <param name="SourceStatus">"submitted", "draft" or "pending": how firm the source is.</param>
public sealed record InstitutionProfile(
    string InstitutionName,
    string? InstitutionKey,
    string? RectorName,
    string? Sostenimiento,
    string? Municipality,
    string? State,
    string? ResponsibleEmail,
    string? SourceCycle,
    string SourceStatus)
{
    /// <summary>Name used on headers while no capture exists yet.</summary>
    public const string DefaultName = "Universidad Politécnica de la Zona Metropolitana de Guadalajara";

    public const string SourceReportType = "Institucion911_10";

    public static readonly InstitutionProfile Unreported =
        new(DefaultName, null, null, null, null, null, null, null, "pending");

    public bool IsReported => SourceCycle is not null;

    public bool IsSubmitted => SourceStatus == "submitted";
}
