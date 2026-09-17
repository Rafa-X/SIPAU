using System.Text.Json.Serialization;

namespace UPZMG.Implementations.Reports;

/// <summary>
/// An official statistical format (911, SII, Anexo TIC) described as data.
/// Each template is a JSON file shipped with the API, so a new cycle's format
/// is a new file, not new code.
/// </summary>
public sealed class ReportTemplateDefinition
{
    [JsonPropertyName("reportType")]
    public string ReportType { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("sections")]
    public List<ReportTemplateSection> Sections { get; set; } = [];
}

public sealed class ReportTemplateSection
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("questions")]
    public List<ReportTemplateQuestion> Questions { get; set; } = [];
}

/// <summary>
/// One reactivo of an official form. The arithmetic the paper form demands
/// (totals, sums that must match) is declared here instead of coded per template.
/// </summary>
public sealed class ReportTemplateQuestion
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>One of <see cref="ReportQuestionTypes"/>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("helpText")]
    public string? HelpText { get; set; }

    [JsonPropertyName("rows")]
    public List<string> Rows { get; set; } = [];

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = [];

    /// <summary>Splits every matrix column, e.g. ["Mujeres", "Hombres"].</summary>
    [JsonPropertyName("subColumns")]
    public List<string> SubColumns { get; set; } = [];

    /// <summary>Choices for select, checkbox_group and matrix_select.</summary>
    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = [];

    /// <summary>Fills <see cref="Options"/> from an official catalogue (see <see cref="ReportCatalogs"/>).</summary>
    [JsonPropertyName("optionsFrom")]
    public string? OptionsFrom { get; set; }

    /// <summary>Fills <see cref="Rows"/> from an official catalogue.</summary>
    [JsonPropertyName("rowsFrom")]
    public string? RowsFrom { get; set; }

    /// <summary>Option that opens a free-text "Especifique" box (e.g. "Otro").</summary>
    [JsonPropertyName("specifyOption")]
    public string? SpecifyOption { get; set; }

    /// <summary>What the numbers mean (see <see cref="ReportMeasures"/>). Averages and percentages never add up.</summary>
    [JsonPropertyName("measure")]
    public string? Measure { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    /// <summary>Read-only total of other questions: "Total = a + b + c".</summary>
    [JsonPropertyName("sum")]
    public List<string> Sum { get; set; } = [];

    /// <summary>Adds a computed total at the end of each matrix row.</summary>
    [JsonPropertyName("rowTotal")]
    public bool RowTotal { get; set; }

    /// <summary>Columns the row total adds up; empty means all of them.</summary>
    [JsonPropertyName("rowTotalColumns")]
    public List<string> RowTotalColumns { get; set; } = [];

    [JsonPropertyName("rowTotalLabel")]
    public string? RowTotalLabel { get; set; }

    /// <summary>Adds a footer row with the total of each column.</summary>
    [JsonPropertyName("columnTotal")]
    public bool ColumnTotal { get; set; }

    /// <summary>Adds a computed total after the sub-columns of each column.</summary>
    [JsonPropertyName("subTotal")]
    public bool SubTotal { get; set; }

    /// <summary>
    /// Cuadratura: this question's total must equal the referenced one. A reference is a
    /// question key ("p2") or one column or row of a matrix ("p2[Total de LIIADT]").
    /// </summary>
    [JsonPropertyName("sumEquals")]
    public string? SumEquals { get; set; }

    /// <summary>Like <see cref="SumEquals"/>, but the total may stay below the reference.</summary>
    [JsonPropertyName("sumAtMost")]
    public string? SumAtMost { get; set; }

    /// <summary>Shown (and required) only while the referenced yes/no answer is "true".</summary>
    [JsonPropertyName("showIf")]
    public string? ShowIf { get; set; }

    /// <summary>Columns of a table question, whose rows the user adds one by one.</summary>
    [JsonPropertyName("fields")]
    public List<ReportTemplateField> Fields { get; set; } = [];

    [JsonPropertyName("minRows")]
    public int? MinRows { get; set; }

    [JsonPropertyName("maxRows")]
    public int? MaxRows { get; set; }
}

/// <summary>A column of a table question.</summary>
public sealed class ReportTemplateField
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>text, number, date or select.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = ReportQuestionTypes.Text;

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = [];
}

/// <summary>A template as listed in the catalogue. Any problem keeps it from being captured.</summary>
public sealed record ReportTemplateItem(
    string ReportType,
    string Version,
    string DisplayName,
    int Sections,
    int Questions,
    IReadOnlyList<string> Problems)
{
    public bool IsValid => Problems.Count == 0;
}

public static class ReportQuestionTypes
{
    public const string Text = "text";
    public const string Number = "number";
    public const string Boolean = "boolean";
    public const string Date = "date";
    public const string Select = "select";
    public const string CheckboxGroup = "checkbox_group";
    public const string Matrix = "matrix";
    public const string MatrixSelect = "matrix_select";
    public const string Table = "table";

    public static readonly string[] All = [Text, Number, Boolean, Date, Select, CheckboxGroup, Matrix, MatrixSelect, Table];

    /// <summary>Types a column of a table question can take.</summary>
    public static readonly string[] FieldTypes = [Text, Number, Date, Select];

    public static string Of(ReportTemplateQuestion question) => question.Type.Trim().ToLowerInvariant();
}

public static class ReportMeasures
{
    /// <summary>Whole things being counted: people, classrooms, computers. They add up.</summary>
    public const string Count = "count";

    /// <summary>Money, hectares and the like: they add up, but they are not whole numbers.</summary>
    public const string Amount = "amount";

    public const string Average = "average";
    public const string Percent = "percent";

    public static readonly string[] All = [Count, Amount, Average, Percent];

    /// <summary>Only counts and amounts may be totalled; averaging or percentage columns never are.</summary>
    public static bool AddsUp(ReportTemplateQuestion question) => Of(question) is Count or Amount;

    /// <summary>A question without a measure holds counts.</summary>
    public static string Of(ReportTemplateQuestion question) =>
        string.IsNullOrWhiteSpace(question.Measure) ? Count : question.Measure.Trim().ToLowerInvariant();
}

/// <summary>Official catalogues a template can name in optionsFrom / rowsFrom.</summary>
public static class ReportCatalogs
{
    public const string Disabilities = "discapacidades";
    public const string WithdrawReasons = "causas_baja";
    public const string Scholarships = "becas";
    public const string IndigenousLanguages = "lenguas_indigenas";
    public const string Periods = "periodos";

    public static readonly string[] All = [Disabilities, WithdrawReasons, Scholarships, IndigenousLanguages, Periods];
}
