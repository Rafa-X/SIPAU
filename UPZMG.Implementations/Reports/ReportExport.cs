namespace UPZMG.Implementations.Reports;

/// <summary>
/// Turns a capture into workbook sheets: a cover plus one tab per section.
///
/// Deliberately apart from <see cref="XlsxExport"/>: that one knows the file
/// format and nothing about official forms, this one knows the shape of a
/// formato and nothing about zip parts. A ninth template needs no change in
/// either, because the sheets come from the same declaration the screen renders.
/// </summary>
public static class ReportExport
{
    public static byte[] ToWorkbook(ReportFormState form) => XlsxExport.Build(ToSheets(form));

    public static IReadOnlyList<XlsxSheet> ToSheets(ReportFormState form)
    {
        var sheets = new List<XlsxSheet> { CoverSheet(form) };

        for (var i = 0; i < form.Template.Sections.Count; i++)
            sheets.Add(SectionSheet(form, form.Template.Sections[i], i + 1));

        return sheets;
    }

    private static XlsxSheet CoverSheet(ReportFormState form)
    {
        var template = form.Template;

        // Collection expression, not a { } initializer: inside the latter a
        // leading [ reads as an indexer assignment, not as a nested list.
        List<List<XlsxCell>> rows =
        [
            [XlsxCell.Label(template.DisplayName, bold: true)],
            [],
            [XlsxCell.Label("Formato", bold: true), XlsxCell.Label(template.ReportType)],
            [XlsxCell.Label("Versión", bold: true), XlsxCell.Label(template.Version)],
            [XlsxCell.Label("Generado", bold: true), XlsxCell.Label(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))],
            [],
            [XlsxCell.Label("Respuestas obligatorias", bold: true), XlsxCell.Value(form.TotalRequired)],
            [XlsxCell.Label("Capturadas", bold: true), XlsxCell.Value(form.AnsweredRequired)],
        ];

        // Exporting a capture that does not add up is legitimate — you export it
        // precisely to review it — but the workbook has to say so, or the file
        // looks official when it is not.
        if (form.Violations.Count > 0)
        {
            rows.Add([]);
            rows.Add([XlsxCell.Label("Diferencias por resolver", bold: true)]);
            foreach (var violation in form.Violations)
                rows.Add([XlsxCell.Label(violation.QuestionText), XlsxCell.Label(violation.Message)]);
        }

        return new XlsxSheet("Portada", rows);
    }

    private static XlsxSheet SectionSheet(ReportFormState form, ReportTemplateSection section, int number)
    {
        List<List<XlsxCell>> rows = [[XlsxCell.Label($"{number}. {section.Title}", bold: true)]];

        if (!string.IsNullOrWhiteSpace(section.Description))
            rows.Add([XlsxCell.Label(section.Description)]);

        rows.Add([]);

        foreach (var question in section.Questions.Where(form.IsVisible))
        {
            switch (ReportQuestionTypes.Of(question))
            {
                case ReportQuestionTypes.Matrix:
                    AppendMatrix(rows, form, question);
                    break;

                case ReportQuestionTypes.MatrixSelect:
                    AppendMatrixSelect(rows, form, question);
                    break;

                case ReportQuestionTypes.Table:
                    AppendTable(rows, form, question);
                    break;

                case ReportQuestionTypes.CheckboxGroup:
                    rows.Add([XlsxCell.Label(question.Text, bold: true), XlsxCell.Label(form.CheckedSummary(question.Key))]);
                    AppendSpecify(rows, form, question);
                    break;

                case ReportQuestionTypes.Boolean:
                    rows.Add([XlsxCell.Label(question.Text, bold: true), XlsxCell.Label(BooleanLabel(form.Get(question.Key)))]);
                    break;

                default:
                    // A computed total is derived, not stored: export the derived
                    // value so the workbook matches what was on screen.
                    rows.Add([
                        XlsxCell.Label(question.Text, bold: true),
                        question.Sum.Count > 0
                            ? XlsxCell.Value(form.ComputedTotal(question))
                            : XlsxCell.Answer(form.Get(question.Key), numeric: ReportQuestionTypes.Of(question) == ReportQuestionTypes.Number),
                    ]);
                    AppendSpecify(rows, form, question);
                    break;
            }

            rows.Add([]);
        }

        return new XlsxSheet($"{number}. {section.Title}", rows);
    }

    private static void AppendMatrix(List<List<XlsxCell>> rows, ReportFormState form, ReportTemplateQuestion question)
    {
        rows.Add([XlsxCell.Label(question.Text, bold: true)]);

        var hasSubs = question.SubColumns.Count > 0;

        var header = new List<XlsxCell> { XlsxCell.Label("Concepto", bold: true) };
        foreach (var column in question.Columns)
        {
            if (!hasSubs)
            {
                header.Add(XlsxCell.Label(column, bold: true));
                continue;
            }

            // A spreadsheet has no merged headers here, so each sub-column keeps
            // its full name: "Cuerpos académicos · Mujeres".
            foreach (var sub in question.SubColumns)
                header.Add(XlsxCell.Label($"{column} · {sub}", bold: true));

            if (question.SubTotal) header.Add(XlsxCell.Label($"{column} · Total", bold: true));
        }

        if (question.RowTotal) header.Add(XlsxCell.Label(question.RowTotalLabel ?? "Total", bold: true));
        rows.Add(header);

        foreach (var row in question.Rows)
        {
            var line = new List<XlsxCell> { XlsxCell.Label(row) };

            foreach (var column in question.Columns)
            {
                if (!hasSubs)
                {
                    line.Add(XlsxCell.Answer(form.Get(ReportFormState.CellKey(question.Key, row, column)), numeric: true));
                    continue;
                }

                foreach (var sub in question.SubColumns)
                    line.Add(XlsxCell.Answer(form.Get(ReportFormState.CellKey(question.Key, row, column, sub)), numeric: true));

                if (question.SubTotal) line.Add(XlsxCell.Value(form.CellTotal(question, row, column)));
            }

            if (question.RowTotal) line.Add(XlsxCell.Value(form.RowTotal(question, row)));
            rows.Add(line);
        }

        if (!question.ColumnTotal || question.Rows.Count <= 1) return;

        var totals = new List<XlsxCell> { XlsxCell.Label("Total", bold: true) };
        foreach (var column in question.Columns)
        {
            if (!hasSubs)
            {
                totals.Add(XlsxCell.Value(form.ColumnTotal(question, column)));
                continue;
            }

            foreach (var sub in question.SubColumns)
                totals.Add(XlsxCell.Value(form.ColumnTotal(question, column, sub)));

            if (question.SubTotal) totals.Add(XlsxCell.Value(form.ColumnTotal(question, column)));
        }

        if (question.RowTotal) totals.Add(XlsxCell.Value(form.RowTotalsSum(question)));
        rows.Add(totals);
    }

    private static void AppendMatrixSelect(List<List<XlsxCell>> rows, ReportFormState form, ReportTemplateQuestion question)
    {
        rows.Add([XlsxCell.Label(question.Text, bold: true)]);
        rows.Add([XlsxCell.Label("Concepto", bold: true), XlsxCell.Label("Respuesta", bold: true)]);

        foreach (var row in question.Rows)
            rows.Add([XlsxCell.Label(row), XlsxCell.Label(form.Get(ReportFormState.RowKey(question.Key, row)))]);
    }

    private static void AppendTable(List<List<XlsxCell>> rows, ReportFormState form, ReportTemplateQuestion question)
    {
        rows.Add([XlsxCell.Label(question.Text, bold: true)]);

        var header = new List<XlsxCell> { XlsxCell.Label("#", bold: true) };
        header.AddRange(question.Fields.Select(field => XlsxCell.Label(field.Text, bold: true)));
        rows.Add(header);

        var count = form.TableRowCount(question);
        for (var r = 1; r <= count; r++)
        {
            var line = new List<XlsxCell> { XlsxCell.Value(r) };
            line.AddRange(question.Fields.Select(field =>
                XlsxCell.Answer(form.Get(ReportFormState.TableCellKey(question.Key, r, field.Key)),
                    numeric: field.Type.Trim().Equals(ReportQuestionTypes.Number, StringComparison.OrdinalIgnoreCase))));
            rows.Add(line);
        }
    }

    private static void AppendSpecify(List<List<XlsxCell>> rows, ReportFormState form, ReportTemplateQuestion question)
    {
        if (string.IsNullOrWhiteSpace(question.SpecifyOption)) return;

        var specified = form.Get(ReportFormState.SpecifyKey(question.Key));
        if (!string.IsNullOrWhiteSpace(specified))
            rows.Add([XlsxCell.Label("Especifique"), XlsxCell.Label(specified)]);
    }

    private static string BooleanLabel(string raw) => raw switch
    {
        "true" => "Sí",
        "false" => "No",
        _ => string.Empty,
    };
}
