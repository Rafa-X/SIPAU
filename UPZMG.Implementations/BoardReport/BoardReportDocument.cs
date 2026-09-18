namespace UPZMG.Implementations.BoardReport;

/// <summary>
/// The board report (Informe de Junta Directiva) as a document.
///
/// It captures nothing of its own: every figure comes from the official formats the areas
/// already sent or from the operational data, and the only hand-written parts are the
/// narrative blocks. Structure and wording are fixed across cycles; the numbers change.
/// </summary>
public sealed record BoardReportDocument(
    string Cycle,
    string Title,
    string InstitutionName,
    string? RectorName,
    string? Location,
    string Status,
    DateTime? SubmittedAtUtc,
    DateTime GeneratedAtUtc,
    IReadOnlyList<BoardReportChapter> Chapters,
    IReadOnlyList<BoardReportSource> Sources)
{
    public bool IsSubmitted => Status == BoardReportStatus.Submitted;

    /// <summary>Formats the report expects but nobody has sent yet.</summary>
    public IReadOnlyList<BoardReportSource> Missing => [.. Sources.Where(s => !s.IsSubmitted)];

    public int NarrativeBlocks => Chapters.Sum(c => c.Blocks.Count(b => b.Kind == BoardReportBlockKinds.Narrative));
    public int PendingNarratives => Chapters.Sum(c => c.Blocks.Count(b => b.Kind == BoardReportBlockKinds.Narrative && b.IsSuggested));
}

public static class BoardReportStatus
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
}

public static class BoardReportBlockKinds
{
    /// <summary>Text written by university staff; the report suggests wording until they replace it.</summary>
    public const string Narrative = "narrative";
    public const string Figures = "figures";
    public const string Table = "table";
    public const string Chart = "chart";
    /// <summary>Says which capture is missing, instead of showing numbers nobody sent.</summary>
    public const string Pending = "pending";
}

public sealed record BoardReportChapter(
    string Number,
    string Title,
    string Summary,
    IReadOnlyList<BoardReportBlock> Blocks);

/// <param name="BlockKey">Where a narrative is stored; null for every other kind.</param>
/// <param name="IsSuggested">True while the narrative is still the wording the system proposed.</param>
/// <param name="Source">Where the figures came from, shown under the block.</param>
public sealed record BoardReportBlock(
    string Kind,
    string? Title = null,
    string? Text = null,
    string? BlockKey = null,
    bool IsSuggested = false,
    string? Source = null,
    string? Note = null,
    IReadOnlyList<BoardReportFigure>? Figures = null,
    BoardReportTable? Table = null,
    BoardReportChart? Chart = null);

public sealed record BoardReportFigure(string Label, string Value, string? Detail = null);

/// <param name="Total">Row of totals, when the form adds its columns up.</param>
public sealed record BoardReportTable(
    IReadOnlyList<string> Columns,
    IReadOnlyList<BoardReportRow> Rows,
    IReadOnlyList<string>? Total = null);

public sealed record BoardReportRow(string Label, IReadOnlyList<string> Values);

/// <param name="Kind">line or bar.</param>
/// <param name="ForecastFromIndex">First projected point of a line, when there is one.</param>
public sealed record BoardReportChart(
    string Kind,
    IReadOnlyList<string> Labels,
    IReadOnlyList<BoardReportSeries> Series,
    int? ForecastFromIndex = null);

/// <param name="Colour">teal, navy, indigo or ochre; the web maps it to the palette.</param>
public sealed record BoardReportSeries(string Name, IReadOnlyList<double> Values, string Colour, bool Dashed = false);

/// <summary>An official format the report consolidates, and whether it has been sent.</summary>
public sealed record BoardReportSource(
    string ReportType,
    string DisplayName,
    string Status,
    DateTime? LastModified,
    string? CapturedBy)
{
    public bool IsSubmitted => Status == "submitted";
}

/// <summary>One cycle in the board report listing.</summary>
public sealed record BoardReportListItem(
    string Cycle,
    string Status,
    DateTime? LastModified,
    int SubmittedFormats,
    int TotalFormats,
    int PendingNarratives);

public class SaveBoardNarrative
{
    public required Guid RequesterKey { get; set; }
    public required string Cycle { get; set; }
    public required string BlockKey { get; set; }
    /// <summary>Empty text restores the wording the system suggests.</summary>
    public string? Text { get; set; }
}

public class SubmitBoardReport
{
    public required Guid RequesterKey { get; set; }
    public required string Cycle { get; set; }
}

/// <param name="Saved">False when the report was already submitted, or the cycle does not exist.</param>
public sealed record BoardReportSaveResult(bool Saved, string? Message = null)
{
    public static BoardReportSaveResult Ok() => new(true);
    public static BoardReportSaveResult Refused(string message) => new(false, message);
}
