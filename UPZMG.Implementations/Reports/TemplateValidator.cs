namespace UPZMG.Implementations.Reports;

/// <summary>One problem found in a template, and where.</summary>
public sealed record TemplateProblem(string Where, string Message)
{
    public override string ToString() => $"{Where}: {Message}";
}

public sealed record TemplateValidation(IReadOnlyList<TemplateProblem> Problems)
{
    public bool IsValid => Problems.Count == 0;
}

/// <summary>
/// Checks that a template can actually be captured. The form engine is declarative,
/// so a broken template does not fail at compile time: it fails in front of the
/// área on the deadline, as a table with no columns or a total pointing nowhere.
/// Pure and static; these rules are the contract of <see cref="ReportFormState"/>.
/// </summary>
public static class TemplateValidator
{
    // Column sizes of reports.ReportType and reports.TemplateVersion.
    private const int MaxReportTypeLength = 40;
    private const int MaxVersionLength = 20;

    private static bool HasNullEntries(ReportTemplateDefinition template) =>
        template.Sections.Any(section => section is null || section.Questions.Any(q => q is null
            || q.Rows.Contains(null!) || q.Columns.Contains(null!) || q.SubColumns.Contains(null!)
            || q.Options.Contains(null!) || q.Sum.Contains(null!) || q.RowTotalColumns.Contains(null!)
            || q.Fields.Contains(null!)));

    public static TemplateValidation Validate(ReportTemplateDefinition template)
    {
        var problems = new List<TemplateProblem>();
        var questions = new Dictionary<string, ReportTemplateQuestion>(StringComparer.OrdinalIgnoreCase);
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // JSON allows a null where a list item goes, and nothing below can read one.
        if (HasNullEntries(template))
            return new TemplateValidation([new TemplateProblem("Plantilla", "Hay una sección, un reactivo o un renglón vacío (null): quítalo.")]);

        ValidateHeader(template, problems);

        if (template.Sections.Count == 0)
            problems.Add(new TemplateProblem("Plantilla", "Agrega al menos una sección."));

        for (var s = 0; s < template.Sections.Count; s++)
        {
            var section = template.Sections[s];
            var where = $"Sección {s + 1}";

            if (string.IsNullOrWhiteSpace(section.Title))
                problems.Add(new TemplateProblem(where, "La sección necesita un título."));

            if (section.Questions.Count == 0)
                problems.Add(new TemplateProblem(where, "La sección no tiene reactivos."));

            foreach (var q in section.Questions)
                ValidateQuestion(q, $"{where} · {Describe(q)}", questions, owners, problems);
        }

        ValidateReferences(template, questions, problems);
        return new TemplateValidation(problems);
    }

    /// <summary>Report type and version form the file name and the URL, so they stay plain ASCII.</summary>
    public static bool IsSafeName(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= maxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

    public static bool IsSafeReportType(string? value) => IsSafeName(value, MaxReportTypeLength);

    public static bool IsSafeVersion(string? value) => IsSafeName(value, MaxVersionLength);

    private static void ValidateHeader(ReportTemplateDefinition t, List<TemplateProblem> problems)
    {
        if (!IsSafeReportType(t.ReportType))
            problems.Add(new TemplateProblem("Plantilla",
                $"La clave del formato (reportType) es obligatoria, de hasta {MaxReportTypeLength} caracteres, y sólo admite letras sin acento, números, guion y guion bajo."));

        if (!IsSafeVersion(t.Version))
            problems.Add(new TemplateProblem("Plantilla",
                $"La versión es obligatoria (por ejemplo 2024-2025), de hasta {MaxVersionLength} caracteres, con letras sin acento, números y guiones."));

        if (string.IsNullOrWhiteSpace(t.DisplayName))
            problems.Add(new TemplateProblem("Plantilla", "Falta el nombre visible del formato."));
    }

    private static void ValidateQuestion(
        ReportTemplateQuestion q, string where,
        Dictionary<string, ReportTemplateQuestion> questions,
        Dictionary<string, string> owners,
        List<TemplateProblem> problems)
    {
        if (string.IsNullOrWhiteSpace(q.Key))
            problems.Add(new TemplateProblem(where, "El reactivo necesita una clave."));
        else if (HasBrackets(q.Key))
            problems.Add(new TemplateProblem(where, "La clave no puede llevar corchetes: se reservan para las celdas."));
        else if (owners.TryGetValue(q.Key, out var previous))
            // Two questions sharing a key silently overwrite each other's answers.
            problems.Add(new TemplateProblem(where, $"La clave «{q.Key}» ya se usa en {previous}."));
        else
        {
            owners[q.Key] = where;
            questions[q.Key] = q;
        }

        if (string.IsNullOrWhiteSpace(q.Text))
            problems.Add(new TemplateProblem(where, "El reactivo necesita un enunciado."));

        var type = ReportQuestionTypes.Of(q);
        if (!ReportQuestionTypes.All.Contains(type))
        {
            problems.Add(new TemplateProblem(where, $"Tipo «{q.Type}» desconocido. Permitidos: {string.Join(", ", ReportQuestionTypes.All)}."));
            return;
        }

        var measure = ReportMeasures.Of(q);
        if (!ReportMeasures.All.Contains(measure))
            problems.Add(new TemplateProblem(where, $"Medida «{q.Measure}» desconocida. Permitidas: {string.Join(", ", ReportMeasures.All)}."));
        else if (!ReportMeasures.AddsUp(q)
                 && (q.RowTotal || q.ColumnTotal || q.SubTotal || q.Sum.Count > 0
                     || !string.IsNullOrWhiteSpace(q.SumEquals) || !string.IsNullOrWhiteSpace(q.SumAtMost)))
            problems.Add(new TemplateProblem(where,
                "Los promedios y porcentajes no se suman: quita rowTotal, columnTotal, subTotal y las reglas de suma."));

        switch (type)
        {
            case ReportQuestionTypes.Matrix:
                RequireRows(q, where, problems);
                if (q.Columns.Count == 0)
                    problems.Add(new TemplateProblem(where, "Una matriz necesita columnas."));
                if (q.Rows.Concat(q.Columns).Concat(q.SubColumns).Any(HasBrackets))
                    problems.Add(new TemplateProblem(where, "Los renglones y columnas no pueden llevar corchetes."));
                if (q.RowTotalColumns.Any(c => !q.Columns.Contains(c, StringComparer.OrdinalIgnoreCase)))
                    problems.Add(new TemplateProblem(where, "rowTotalColumns menciona columnas que la matriz no tiene."));
                if (q.RowTotalColumns.Count > 0 && !q.RowTotal)
                    problems.Add(new TemplateProblem(where, "rowTotalColumns sólo tiene efecto con rowTotal."));
                if (q.SubTotal && q.SubColumns.Count == 0)
                    problems.Add(new TemplateProblem(where, "subTotal necesita subColumns."));
                break;

            case ReportQuestionTypes.MatrixSelect:
                RequireRows(q, where, problems);
                RequireOptions(q, where, problems);
                break;

            case ReportQuestionTypes.Select or ReportQuestionTypes.CheckboxGroup:
                RequireOptions(q, where, problems);
                break;

            case ReportQuestionTypes.Table:
                ValidateTable(q, where, problems);
                break;
        }

        if (type != ReportQuestionTypes.Matrix && (q.RowTotal || q.ColumnTotal || q.SubTotal || q.SubColumns.Count > 0))
            problems.Add(new TemplateProblem(where, "Los totales por renglón o columna y las subcolumnas sólo aplican a matrices."));

        if (q.Sum.Count > 0 && type != ReportQuestionTypes.Number)
            problems.Add(new TemplateProblem(where, "Un total calculado (sum) debe ser de tipo number."));

        foreach (var catalog in new[] { q.OptionsFrom, q.RowsFrom })
            if (!string.IsNullOrWhiteSpace(catalog) && !ReportCatalogs.All.Contains(catalog.Trim(), StringComparer.OrdinalIgnoreCase))
                problems.Add(new TemplateProblem(where, $"Catálogo «{catalog}» desconocido. Disponibles: {string.Join(", ", ReportCatalogs.All)}."));

        if (!string.IsNullOrWhiteSpace(q.SpecifyOption) && q.Options.Count > 0
            && !q.Options.Contains(q.SpecifyOption, StringComparer.OrdinalIgnoreCase))
            problems.Add(new TemplateProblem(where, $"«{q.SpecifyOption}» no está entre las opciones, así que «Especifique» nunca aparecería."));

        if (q.Min is { } min && q.Max is { } max && min > max)
            problems.Add(new TemplateProblem(where, "El mínimo no puede ser mayor que el máximo."));
    }

    private static void ValidateTable(ReportTemplateQuestion q, string where, List<TemplateProblem> problems)
    {
        if (q.Fields.Count == 0)
            problems.Add(new TemplateProblem(where, "Una tabla necesita al menos un campo (fields)."));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in q.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Key) || HasBrackets(field.Key) || !seen.Add(field.Key))
                problems.Add(new TemplateProblem(where, $"El campo «{field.Key}» necesita una clave única y sin corchetes."));

            if (string.IsNullOrWhiteSpace(field.Text))
                problems.Add(new TemplateProblem(where, $"El campo «{field.Key}» necesita un encabezado."));

            var fieldType = field.Type.Trim().ToLowerInvariant();
            if (!ReportQuestionTypes.FieldTypes.Contains(fieldType))
                problems.Add(new TemplateProblem(where, $"El campo «{field.Key}» tiene un tipo no admitido. Permitidos: {string.Join(", ", ReportQuestionTypes.FieldTypes)}."));
            else if (fieldType == ReportQuestionTypes.Select && field.Options.Count == 0)
                problems.Add(new TemplateProblem(where, $"El campo «{field.Key}» es una lista y necesita opciones."));
        }

        if (q.MaxRows is < 1)
            problems.Add(new TemplateProblem(where, "maxRows debe ser al menos 1."));

        if (q.MinRows is { } minRows && (minRows < 0 || (q.MaxRows is { } maxRows && minRows > maxRows)))
            problems.Add(new TemplateProblem(where, "minRows debe estar entre 0 y maxRows."));
    }

    /// <summary>
    /// Cross-question references are checked once every key is known, so the order
    /// of questions inside the template does not matter.
    /// </summary>
    private static void ValidateReferences(
        ReportTemplateDefinition template,
        IReadOnlyDictionary<string, ReportTemplateQuestion> questions,
        List<TemplateProblem> problems)
    {
        for (var s = 0; s < template.Sections.Count; s++)
        {
            foreach (var q in template.Sections[s].Questions)
            {
                var where = $"Sección {s + 1} · {Describe(q)}";

                foreach (var reference in q.Sum)
                    RequireReference(reference, "sum", where, questions, problems);

                if (!string.IsNullOrWhiteSpace(q.SumEquals))
                    RequireReference(q.SumEquals, "sumEquals", where, questions, problems);

                if (!string.IsNullOrWhiteSpace(q.SumAtMost))
                    RequireReference(q.SumAtMost, "sumAtMost", where, questions, problems);

                if (!string.IsNullOrWhiteSpace(q.ShowIf))
                {
                    if (!questions.TryGetValue(q.ShowIf, out var target))
                        problems.Add(new TemplateProblem(where, $"«showIf» apunta a «{q.ShowIf}», que no existe en la plantilla."));
                    else if (ReportQuestionTypes.Of(target) != ReportQuestionTypes.Boolean)
                        problems.Add(new TemplateProblem(where, "«showIf» debe apuntar a una pregunta de sí o no."));
                }

                if (SelfReference(q))
                    problems.Add(new TemplateProblem(where, "Una regla de suma no puede apuntar a su propio reactivo."));
            }
        }
    }

    private static void RequireReference(
        string reference, string field, string where,
        IReadOnlyDictionary<string, ReportTemplateQuestion> questions,
        List<TemplateProblem> problems)
    {
        if (questions.ContainsKey(reference) || ReportFormState.TryMatrixReference(reference, questions, out _, out _, out _)) return;

        problems.Add(new TemplateProblem(where,
            $"«{field}» apunta a «{reference}», que no es una clave, ni una columna o renglón de matriz de la plantilla."));
    }

    private static bool SelfReference(ReportTemplateQuestion q) =>
        !string.IsNullOrWhiteSpace(q.Key)
        && (q.Sum.Contains(q.Key, StringComparer.OrdinalIgnoreCase)
            || q.Key.Equals(q.SumEquals, StringComparison.OrdinalIgnoreCase)
            || q.Key.Equals(q.SumAtMost, StringComparison.OrdinalIgnoreCase));

    private static void RequireRows(ReportTemplateQuestion q, string where, List<TemplateProblem> problems)
    {
        if (q.Rows.Count == 0 && string.IsNullOrWhiteSpace(q.RowsFrom))
            problems.Add(new TemplateProblem(where, "Necesita renglones (rows) o un catálogo en rowsFrom."));
    }

    private static void RequireOptions(ReportTemplateQuestion q, string where, List<TemplateProblem> problems)
    {
        if (q.Options.Count == 0 && string.IsNullOrWhiteSpace(q.OptionsFrom))
            problems.Add(new TemplateProblem(where, "Necesita opciones (options) o un catálogo en optionsFrom."));
    }

    private static bool HasBrackets(string value) => value.IndexOfAny(['[', ']']) >= 0;

    private static string Describe(ReportTemplateQuestion q) =>
        string.IsNullOrWhiteSpace(q.Key) ? "(sin clave)" : q.Key;
}
