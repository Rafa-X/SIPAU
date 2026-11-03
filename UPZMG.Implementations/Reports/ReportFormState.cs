using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace UPZMG.Implementations.Reports;

/// <summary>A required answer still to be captured, and where to find it.</summary>
public sealed record MissingAnswer(int SectionIndex, string SectionTitle, string QuestionKey, string QuestionText);

/// <summary>A broken rule (cuadratura or value range) and a message that says how to fix it.</summary>
public sealed record RuleViolation(int SectionIndex, string SectionTitle, string QuestionKey, string QuestionText, string Message);

/// <summary>Where one section stands, for the capture index.</summary>
public sealed record SectionProgress(int Required, int Missing, int Violations, bool Touched);

/// <summary>A matrix input cell and a readable name for messages.</summary>
public readonly record struct MatrixCell(string Key, string Row, string Column, string? Sub)
{
    public string Label => Sub is null ? $"{Row} · {Column}" : $"{Row} · {Column} · {Sub}";
}

/// <summary>
/// The answers of one capture and everything derived from them: totals, missing
/// required answers and broken rules. It has no UI and no I/O, so the Web renders
/// it while typing and the API re-checks a submission with the same arithmetic.
/// </summary>
public sealed class ReportFormState
{
    // ASCII unit separator: never part of an official option's text.
    private const char OptionSeparator = '';
    private const string SpecifySuffix = "__especifique";
    private const double Tolerance = 0.001;
    // References may chain (a total of totals); this stops a cycle the validator missed.
    private const int MaxDepth = 16;

    private readonly Dictionary<string, string?> _answers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ReportTemplateQuestion> _questions = new(StringComparer.OrdinalIgnoreCase);
    private SectionProgress[] _sections = [];

    public ReportFormState(ReportTemplateDefinition template, IDictionary<string, string?>? initial = null)
    {
        Template = template;

        if (initial is not null)
            foreach (var (key, value) in initial) _answers[key] = value;

        // First declaration wins; the validator reports duplicated keys.
        foreach (var question in template.Sections.SelectMany(s => s.Questions))
            if (!string.IsNullOrWhiteSpace(question.Key)) _questions.TryAdd(question.Key, question);

        foreach (var question in _questions.Values.Where(q => ReportQuestionTypes.Of(q) == ReportQuestionTypes.CheckboxGroup))
            ReadOptionsWrittenWithSemicolons(question);

        Recompute();
    }

    // The seeded captures join several options with ";". Read as they are, no box showed ticked and
    // the value could not be unticked either, because no box corresponds to it.
    private void ReadOptionsWrittenWithSemicolons(ReportTemplateQuestion q)
    {
        var raw = Get(q.Key);
        if (!raw.Contains(';') || raw.Contains(OptionSeparator) || q.Options.Contains(raw, StringComparer.OrdinalIgnoreCase)) return;

        _answers[q.Key] = string.Join(OptionSeparator, raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public ReportTemplateDefinition Template { get; }

    /// <summary>Raw answers, ready to be saved as they are.</summary>
    public Dictionary<string, string?> Answers => _answers;

    public IReadOnlyList<MissingAnswer> Missing { get; private set; } = [];
    public IReadOnlyList<RuleViolation> Violations { get; private set; } = [];
    public int TotalRequired { get; private set; }
    public int AnsweredRequired { get; private set; }

    public bool IsComplete => Missing.Count == 0 && Violations.Count == 0;

    public double CompletionPercent =>
        TotalRequired == 0 ? 100 : (double)AnsweredRequired / TotalRequired * 100;

    public SectionProgress Progress(int sectionIndex) =>
        sectionIndex >= 0 && sectionIndex < _sections.Length ? _sections[sectionIndex] : new SectionProgress(0, 0, 0, false);

    public IEnumerable<RuleViolation> ViolationsFor(string questionKey) =>
        Violations.Where(v => v.QuestionKey.Equals(questionKey, StringComparison.OrdinalIgnoreCase));

    // ── Answers ─────────────────────────────────────────────────────────────
    public string Get(string key) => _answers.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;

    public bool HasValue(string key) => !string.IsNullOrWhiteSpace(Get(key));

    public void Set(string key, string? value)
    {
        _answers[key] = value?.Trim();
        Recompute();
    }

    // ── Multi-select ────────────────────────────────────────────────────────
    public bool IsChecked(string key, string option) =>
        Get(key).Split(OptionSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Contains(option, StringComparer.OrdinalIgnoreCase);

    public void ToggleOption(string key, string option, bool on)
    {
        var current = Get(key).Split(OptionSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
        current.RemoveAll(o => o.Equals(option, StringComparison.OrdinalIgnoreCase));
        if (on) current.Add(option);
        Set(key, string.Join(OptionSeparator, current));
    }

    public string CheckedSummary(string key) =>
        string.Join(", ", Get(key).Split(OptionSeparator, StringSplitOptions.RemoveEmptyEntries));

    // ── Answer keys ─────────────────────────────────────────────────────────
    public static string CellKey(string questionKey, string row, string column) => $"{questionKey}[{row}][{column}]";

    public static string CellKey(string questionKey, string row, string column, string sub) => $"{questionKey}[{row}][{column}][{sub}]";

    public static string RowKey(string questionKey, string row) => $"{questionKey}[{row}]";

    public static string SpecifyKey(string questionKey) => questionKey + SpecifySuffix;

    // Table rows are numbered from 1; the row count is stored beside them.
    public static string TableCellKey(string questionKey, int row, string field) => $"{questionKey}[{row}][{field}]";

    public static string TableCountKey(string questionKey) => $"{questionKey}[#]";

    public static IEnumerable<MatrixCell> MatrixCells(ReportTemplateQuestion q)
    {
        foreach (var row in q.Rows)
            foreach (var column in q.Columns)
            {
                if (q.SubColumns.Count == 0)
                {
                    yield return new MatrixCell(CellKey(q.Key, row, column), row, column, null);
                    continue;
                }

                foreach (var sub in q.SubColumns)
                    yield return new MatrixCell(CellKey(q.Key, row, column, sub), row, column, sub);
            }
    }

    // ── Numbers and totals ──────────────────────────────────────────────────
    public static bool TryParseNumber(string? raw, out double value) =>
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    public static string Format(double value) => value.ToString("#,0.##", CultureInfo.InvariantCulture);

    public double Number(string key) => TryParseNumber(Get(key), out var n) ? n : 0;

    /// <summary>One column of one row, adding up its sub-columns when it has them.</summary>
    public double CellTotal(ReportTemplateQuestion q, string row, string column) =>
        q.SubColumns.Count == 0
            ? Number(CellKey(q.Key, row, column))
            : q.SubColumns.Sum(sub => Number(CellKey(q.Key, row, column, sub)));

    public static IReadOnlyList<string> TotalledColumns(ReportTemplateQuestion q) =>
        q.RowTotalColumns.Count > 0 ? q.RowTotalColumns : q.Columns;

    public double RowTotal(ReportTemplateQuestion q, string row) =>
        TotalledColumns(q).Sum(column => CellTotal(q, row, column));

    public double RowTotalsSum(ReportTemplateQuestion q) => q.Rows.Sum(row => RowTotal(q, row));

    public double ColumnTotal(ReportTemplateQuestion q, string column) =>
        q.Rows.Sum(row => CellTotal(q, row, column));

    public double ColumnTotal(ReportTemplateQuestion q, string column, string sub) =>
        q.Rows.Sum(row => Number(CellKey(q.Key, row, column, sub)));

    /// <summary>The read-only value of a computed question ("Total = a + b + c").</summary>
    public double ComputedTotal(ReportTemplateQuestion q) => q.Sum.Sum(reference => Resolve(reference, 1));

    /// <summary>Everything captured for a question: matrix cells, table rows, a computed sum or a single value.</summary>
    public double QuestionTotal(ReportTemplateQuestion q) => QuestionTotal(q, 0);

    /// <summary>The value a rule reference points at: a question's total or one matrix column.</summary>
    public double Resolve(string reference) => Resolve(reference, 0);

    /// <summary>Readable name of a rule reference, for messages.</summary>
    public string Describe(string reference)
    {
        if (_questions.TryGetValue(reference, out var q)) return q.Text;
        return TryMatrixReference(reference, _questions, out var matrix, out var name, out _)
            ? $"{name} ({matrix.Text})"
            : reference;
    }

    /// <summary>
    /// Parses "questionKey[Name]" against a matrix question, where Name is one of its
    /// columns or one of its rows. The validator uses it too.
    /// </summary>
    public static bool TryMatrixReference(
        string reference,
        IReadOnlyDictionary<string, ReportTemplateQuestion> questions,
        [NotNullWhen(true)] out ReportTemplateQuestion? matrix,
        out string name,
        out bool isColumn)
    {
        matrix = null;
        name = string.Empty;
        isColumn = false;

        var open = reference.IndexOf('[');
        if (open <= 0 || !reference.EndsWith(']')) return false;
        if (!questions.TryGetValue(reference[..open], out var q) || ReportQuestionTypes.Of(q) != ReportQuestionTypes.Matrix) return false;

        var wanted = reference[(open + 1)..^1];

        var column = q.Columns.FirstOrDefault(c => c.Equals(wanted, StringComparison.OrdinalIgnoreCase));
        if (column is not null)
        {
            matrix = q;
            name = column;
            isColumn = true;
            return true;
        }

        var row = q.Rows.FirstOrDefault(r => r.Equals(wanted, StringComparison.OrdinalIgnoreCase));
        if (row is null) return false;

        matrix = q;
        name = row;
        return true;
    }

    private double QuestionTotal(ReportTemplateQuestion q, int depth)
    {
        if (depth > MaxDepth) return 0;

        return ReportQuestionTypes.Of(q) switch
        {
            ReportQuestionTypes.Matrix => q.Rows.Sum(row => q.Columns.Sum(column => CellTotal(q, row, column))),
            ReportQuestionTypes.Table => TableRowCount(q),
            _ when q.Sum.Count > 0 => q.Sum.Sum(reference => Resolve(reference, depth + 1)),
            _ => Number(q.Key),
        };
    }

    private double Resolve(string reference, int depth)
    {
        if (depth > MaxDepth) return 0;
        if (_questions.TryGetValue(reference, out var q)) return QuestionTotal(q, depth + 1);

        if (TryMatrixReference(reference, _questions, out var matrix, out var name, out var isColumn))
            return isColumn ? ColumnTotal(matrix, name) : RowTotal(matrix, name);

        return Number(reference);
    }

    // ── Tables (rows added by the user) ─────────────────────────────────────
    public int TableRowCount(ReportTemplateQuestion q) =>
        TryParseNumber(Get(TableCountKey(q.Key)), out var n) ? (int)Math.Clamp(n, 0, 10_000) : 0;

    public void AddTableRow(ReportTemplateQuestion q)
    {
        var count = TableRowCount(q);
        if (q.MaxRows is { } max && count >= max) return;

        _answers[TableCountKey(q.Key)] = (count + 1).ToString(CultureInfo.InvariantCulture);
        Recompute();
    }

    /// <summary>Removes one row (numbered from 1) and moves the rows below it up.</summary>
    public void RemoveTableRow(ReportTemplateQuestion q, int row)
    {
        var count = TableRowCount(q);
        if (row < 1 || row > count) return;

        for (var r = row; r < count; r++)
            foreach (var field in q.Fields)
                _answers[TableCellKey(q.Key, r, field.Key)] = Get(TableCellKey(q.Key, r + 1, field.Key));

        foreach (var field in q.Fields)
            _answers.Remove(TableCellKey(q.Key, count, field.Key));

        _answers[TableCountKey(q.Key)] = (count - 1).ToString(CultureInfo.InvariantCulture);
        Recompute();
    }

    // ── Matrix helpers ──────────────────────────────────────────────────────
    public int BlankCells(ReportTemplateQuestion q) => MatrixCells(q).Count(cell => !HasValue(cell.Key));

    /// <summary>Writes 0 into every blank cell, so a mostly empty official table is completed in one step.</summary>
    public int FillBlanksWithZero(ReportTemplateQuestion q)
    {
        var filled = 0;
        foreach (var cell in MatrixCells(q))
        {
            if (HasValue(cell.Key)) continue;
            _answers[cell.Key] = "0";
            filled++;
        }

        if (filled > 0) Recompute();
        return filled;
    }

    // ── Submission ──────────────────────────────────────────────────────────
    /// <summary>
    /// Answers the capture screen cannot produce: a value outside a question's list, more rows than
    /// a table allows, a date that is not one. The screen only offers what is valid, so these come
    /// from a request that did not pass through it, or from a draft whose option left the catalogue.
    /// They are kept apart from <see cref="Violations"/>, which is also printed with a format that
    /// was already sent: a catalogue that changes later does not make that document wrong.
    /// </summary>
    public IReadOnlyList<RuleViolation> OutOfCatalogue()
    {
        var problems = new List<RuleViolation>();

        for (var s = 0; s < Template.Sections.Count; s++)
        {
            var section = Template.Sections[s];

            foreach (var q in section.Questions.Where(IsVisible))
                problems.AddRange(CatalogueProblems(q).Select(message => new RuleViolation(s, section.Title, q.Key, q.Text, message)));
        }

        return problems;
    }

    private IEnumerable<string> CatalogueProblems(ReportTemplateQuestion q)
    {
        switch (ReportQuestionTypes.Of(q))
        {
            case ReportQuestionTypes.Select:
                if (IsOutside(q.Options, Get(q.Key))) yield return NotInList(Get(q.Key));
                break;

            case ReportQuestionTypes.CheckboxGroup:
                foreach (var option in Get(q.Key).Split(OptionSeparator, StringSplitOptions.RemoveEmptyEntries))
                    if (IsOutside(q.Options, option)) yield return NotInList(option);
                break;

            case ReportQuestionTypes.MatrixSelect:
                foreach (var row in q.Rows)
                    if (IsOutside(q.Options, Get(RowKey(q.Key, row)))) yield return $"{row}: {NotInList(Get(RowKey(q.Key, row)))}";
                break;

            case ReportQuestionTypes.Boolean:
                if (HasValue(q.Key) && !bool.TryParse(Get(q.Key), out _)) yield return $"«{Get(q.Key)}» no es una respuesta de sí o no.";
                break;

            case ReportQuestionTypes.Date:
                if (HasValue(q.Key) && !IsDate(Get(q.Key))) yield return $"«{Get(q.Key)}» no es una fecha.";
                break;

            case ReportQuestionTypes.Table:
                var rows = TableRowCount(q);
                if (q.MaxRows is { } max && rows > max) yield return $"Hay {rows} líneas y el máximo son {max}.";

                for (var r = 1; r <= rows; r++)
                    foreach (var field in q.Fields)
                        if (FieldProblem(field, Get(TableCellKey(q.Key, r, field.Key))) is { } problem)
                            yield return $"Línea {r}, {field.Text}: {problem}";
                break;
        }
    }

    private static string? FieldProblem(ReportTemplateField field, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return field.Type.Trim().ToLowerInvariant() switch
        {
            ReportQuestionTypes.Select when IsOutside(field.Options, value) => NotInList(value),
            ReportQuestionTypes.Number when !TryParseNumber(value, out _) => $"«{value}» no es un número.",
            ReportQuestionTypes.Date when !IsDate(value) => $"«{value}» no es una fecha.",
            _ => null,
        };
    }

    private static bool IsOutside(List<string> options, string value) =>
        options.Count > 0 && !string.IsNullOrWhiteSpace(value) && !options.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    private static string NotInList(string value) => $"«{value}» no está en la lista de opciones.";

    private static bool IsDate(string value) => DateOnly.TryParse(value, CultureInfo.InvariantCulture, out _);

    // ── Values ──────────────────────────────────────────────────────────────
    /// <summary>Allowed range of a question's values; percentages default to 0–100.</summary>
    public static (double Min, double? Max) Bounds(ReportTemplateQuestion q) =>
        ReportMeasures.Of(q) == ReportMeasures.Percent ? (q.Min ?? 0, q.Max ?? 100) : (q.Min ?? 0, q.Max);

    /// <summary>True for a typed value that is not a number, is out of range, or is a fractional count.</summary>
    public static bool IsInvalidValue(ReportTemplateQuestion q, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (!TryParseNumber(raw, out var n)) return true;

        var (min, max) = Bounds(q);
        if (n < min - Tolerance || (max is { } ceiling && n > ceiling + Tolerance)) return true;

        return ReportMeasures.Of(q) == ReportMeasures.Count && Math.Abs(n - Math.Round(n)) > Tolerance;
    }

    /// <summary>The value rule in plain words, e.g. "un número entero mayor o igual a 0".</summary>
    public static string RuleText(ReportTemplateQuestion q)
    {
        var (min, max) = Bounds(q);
        var kind = ReportMeasures.Of(q) switch
        {
            ReportMeasures.Count => "un número entero",
            ReportMeasures.Percent => "un porcentaje",
            ReportMeasures.Amount => "un importe",
            _ => "un número",
        };

        return max is { } ceiling
            ? $"{kind} entre {Format(min)} y {Format(ceiling)}"
            : $"{kind} mayor o igual a {Format(min)}";
    }

    // ── Visibility ──────────────────────────────────────────────────────────
    public bool IsVisible(ReportTemplateQuestion q) =>
        string.IsNullOrWhiteSpace(q.ShowIf) ||
        Get(q.ShowIf).Equals("true", StringComparison.OrdinalIgnoreCase);

    // ── Completion and rules ────────────────────────────────────────────────
    public void Recompute()
    {
        var missing = new List<MissingAnswer>();
        var violations = new List<RuleViolation>();
        var touched = TouchedQuestionKeys();
        var sections = new SectionProgress[Template.Sections.Count];

        for (var s = 0; s < Template.Sections.Count; s++)
        {
            var section = Template.Sections[s];
            int required = 0, sectionMissing = 0, sectionViolations = 0;
            var sectionTouched = false;

            foreach (var q in section.Questions)
            {
                // A question the user cannot see cannot be demanded of them.
                if (!IsVisible(q)) continue;

                sectionTouched |= touched.Contains(q.Key);

                if (q.Required)
                {
                    required++;
                    if (!IsAnswered(q))
                    {
                        sectionMissing++;
                        missing.Add(new MissingAnswer(s, section.Title, q.Key, q.Text));
                    }
                }

                foreach (var message in CheckRules(q))
                {
                    sectionViolations++;
                    violations.Add(new RuleViolation(s, section.Title, q.Key, q.Text, message));
                }
            }

            sections[s] = new SectionProgress(required, sectionMissing, sectionViolations, sectionTouched);
        }

        _sections = sections;
        Missing = missing;
        Violations = violations;
        TotalRequired = sections.Sum(p => p.Required);
        AnsweredRequired = TotalRequired - missing.Count;
    }

    /// <summary>What is wrong with a question right now: unbalanced sums first, then bad values.</summary>
    public IEnumerable<string> CheckRules(ReportTemplateQuestion q)
    {
        var isTable = ReportQuestionTypes.Of(q) == ReportQuestionTypes.Table;

        if (!string.IsNullOrWhiteSpace(q.SumEquals))
        {
            var captured = QuestionTotal(q);
            var expected = Resolve(q.SumEquals);

            // Nothing captured on either side means incomplete, not wrong.
            if ((captured > 0 || expected > 0) && Math.Abs(captured - expected) > Tolerance)
            {
                yield return isTable
                    ? $"Hay {Format(captured)} {Lines(captured)}; deben ser {Format(expected)} para coincidir con «{Describe(q.SumEquals)}»."
                    : $"La suma capturada ({Format(captured)}) debe ser igual a «{Describe(q.SumEquals)}» ({Format(expected)}).";
            }
        }

        if (!string.IsNullOrWhiteSpace(q.SumAtMost))
        {
            var captured = QuestionTotal(q);
            var ceiling = Resolve(q.SumAtMost);

            if (captured > ceiling + Tolerance)
            {
                yield return isTable
                    ? $"Hay {Format(captured)} {Lines(captured)}; no pueden ser más de {Format(ceiling)} («{Describe(q.SumAtMost)}»)."
                    : $"La suma capturada ({Format(captured)}) no puede exceder «{Describe(q.SumAtMost)}» ({Format(ceiling)}).";
            }
        }

        if (RangeProblem(q) is { } range) yield return range;
    }

    /// <summary>A required question counts as captured only when it is complete; official tables need every cell.</summary>
    public bool IsAnswered(ReportTemplateQuestion q) => IsAnswered(q, 0);

    private bool IsAnswered(ReportTemplateQuestion q, int depth)
    {
        if (depth > MaxDepth) return false;

        switch (ReportQuestionTypes.Of(q))
        {
            case ReportQuestionTypes.Matrix:
                return q.Rows.Count > 0 && q.Columns.Count > 0 && MatrixCells(q).All(cell => HasValue(cell.Key));

            case ReportQuestionTypes.MatrixSelect:
                return q.Rows.Count > 0 && q.Rows.All(row => HasValue(RowKey(q.Key, row)));

            case ReportQuestionTypes.Table:
            {
                var rows = TableRowCount(q);
                return rows >= Math.Max(1, q.MinRows ?? 1)
                    && Enumerable.Range(1, rows).All(r => q.Fields.All(f => HasValue(TableCellKey(q.Key, r, f.Key))));
            }

            case ReportQuestionTypes.CheckboxGroup:
                return HasValue(q.Key) && SpecifyAnswered(q, IsChecked(q.Key, q.SpecifyOption ?? string.Empty));

            case ReportQuestionTypes.Select:
                return HasValue(q.Key) && SpecifyAnswered(q, Get(q.Key).Equals(q.SpecifyOption, StringComparison.OrdinalIgnoreCase));

            default:
                // A computed total is answered once everything it adds up is.
                if (q.Sum.Count > 0) return q.Sum.All(reference => IsReferenceAnswered(reference, depth + 1));
                return HasValue(q.Key);
        }
    }

    private bool SpecifyAnswered(ReportTemplateQuestion q, bool specifyChosen) =>
        string.IsNullOrWhiteSpace(q.SpecifyOption) || !specifyChosen || HasValue(SpecifyKey(q.Key));

    private bool IsReferenceAnswered(string reference, int depth)
    {
        if (_questions.TryGetValue(reference, out var q)) return IsAnswered(q, depth);
        return TryMatrixReference(reference, _questions, out var matrix, out _, out _)
            ? IsAnswered(matrix, depth)
            : HasValue(reference);
    }

    private string? RangeProblem(ReportTemplateQuestion q)
    {
        var type = ReportQuestionTypes.Of(q);
        List<(string Key, string? Label)> cells;

        if (type == ReportQuestionTypes.Matrix)
            cells = MatrixCells(q).Select(cell => (cell.Key, (string?)cell.Label)).ToList();
        else if (type == ReportQuestionTypes.Number && q.Sum.Count == 0)
            cells = [(q.Key, null)];
        else
            return null;

        var bad = cells.Where(cell => IsInvalidValue(q, Get(cell.Key))).ToList();
        if (bad.Count == 0) return null;

        var rule = RuleText(q);
        if (bad.Count > 1)
            return $"{bad.Count} valores deben corregirse, empezando por «{bad[0].Label}»: cada uno debe ser {rule}.";

        return bad[0].Label is null
            ? $"El valor debe ser {rule}."
            : $"«{bad[0].Label}» debe ser {rule}.";
    }

    // Answer keys look like "q", "q[row][col]", "q[#]" or "q__especifique"; the prefix names the question.
    private HashSet<string> TouchedQuestionKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in _answers)
        {
            if (string.IsNullOrWhiteSpace(value) || key.EndsWith("[#]", StringComparison.Ordinal)) continue;

            var bracket = key.IndexOf('[');
            keys.Add(bracket > 0 ? key[..bracket]
                : key.EndsWith(SpecifySuffix, StringComparison.OrdinalIgnoreCase) ? key[..^SpecifySuffix.Length]
                : key);
        }
        return keys;
    }

    private static string Lines(double count) => Math.Abs(count - 1) < Tolerance ? "línea registrada" : "líneas registradas";
}
