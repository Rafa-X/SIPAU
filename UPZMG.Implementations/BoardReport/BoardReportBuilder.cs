using System.Globalization;
using UPZMG.Implementations.Reports;

namespace UPZMG.Implementations.BoardReport;

/// <summary>
/// Composes the board report out of what the institution already has.
///
/// Every figure names its origin: an official format that was sent, or the operational data
/// the system computes. When a format has not been sent, the chapter says which one is missing
/// instead of showing numbers nobody reported.
/// </summary>
public static class BoardReportBuilder
{
    public const string InstitutionName = "Universidad Politécnica de la Zona Metropolitana de Guadalajara";

    private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");

    public static BoardReportDocument Build(BoardReportInputs input, DateTime generatedAtUtc)
    {
        var captures = input.Captures.ToDictionary(c => c.ReportType, StringComparer.OrdinalIgnoreCase);
        var institution = Capture(captures, "Institucion911_10");

        return new BoardReportDocument(
            Cycle: input.Cycle,
            Title: $"Informe a la Junta Directiva · ciclo {input.Cycle}",
            InstitutionName: Text(institution, "nombre_institucion") ?? InstitutionName,
            RectorName: Text(institution, "nombre_rector"),
            Location: Join(" · ", Text(institution, "municipio"), Text(institution, "entidad")),
            Status: input.Status,
            SubmittedAtUtc: input.SubmittedAtUtc,
            GeneratedAtUtc: generatedAtUtc,
            Chapters:
            [
                Presentation(input, captures),
                Enrolment(input, captures),
                Achievement(input, captures),
                Graduation(input, captures),
                Scholarships(input, captures),
                Staff(input, captures),
                Facilities(input, captures),
                Outreach(input, captures),
                Finance(input, captures),
                Outlook(input),
            ],
            Sources: [.. input.Captures.Select(c =>
                new BoardReportSource(c.ReportType, c.DisplayName, c.Status, c.LastModified, c.CapturedBy))]);
    }

    // ── Chapters ────────────────────────────────────────────────────────────

    private static BoardReportChapter Presentation(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var institution = Capture(captures, "Institucion911_10");
        var last = input.History.Periods.Count > 0 ? input.History.Periods[^1] : null;

        var blocks = new List<BoardReportBlock>
        {
            Narrative(input, "presentacion.mensaje", "Mensaje de la Rectoría",
                $"El presente informe da cuenta de la operación de {InstitutionName} durante el ciclo {input.Cycle}, "
                + "en cumplimiento de las atribuciones de la Junta Directiva. Las cifras provienen de los formatos "
                + "estadísticos oficiales que cada área capturó y de los registros académicos del sistema."),
            new BoardReportBlock(
                BoardReportBlockKinds.Figures,
                Title: "Cifras de apertura",
                Source: "Registros del sistema",
                Figures:
                [
                    new("Alumnos activos", Count(input.Dashboard.ActiveStudents),
                        $"{Count(input.Dashboard.TotalStudents)} registrados en total"),
                    new("Matrícula del último periodo", last is null ? "Sin registro" : Count(last.Enrolled),
                        last?.Label),
                    new("Docentes", Count(input.Dashboard.TotalTeachers),
                        $"{Count(input.Dashboard.TeachersWithPostgrad)} con posgrado"),
                    new("Grupos del periodo", Count(input.Dashboard.ClassGroups),
                        $"{Count(input.Dashboard.GroupEnrolled)} inscritos de {Count(input.Dashboard.GroupCapacity)} lugares"),
                ]),
        };

        blocks.Add(institution is { IsSubmitted: true }
            ? new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Datos de identificación",
                Source: SourceOf(institution),
                Table: new BoardReportTable(
                    ["Dato", "Valor"],
                    [
                        Row("Institución", Text(institution, "nombre_institucion") ?? InstitutionName),
                        Row("Clave de institución", Text(institution, "clave_institucion") ?? "—"),
                        Row("Rector", Text(institution, "nombre_rector") ?? "—"),
                        Row("Municipio", Text(institution, "municipio") ?? "—"),
                        Row("Entidad federativa", Text(institution, "entidad") ?? "—"),
                        Row("Sostenimiento", Text(institution, "sostenimiento") ?? "—"),
                    ]))
            : Pending("Datos de identificación", institution, "Formato 911.10 · Institución"));

        return new BoardReportChapter("I", "Presentación institucional",
            "Identificación de la universidad y cifras de apertura del ciclo.", blocks);
    }

    private static BoardReportChapter Enrolment(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var blocks = new List<BoardReportBlock>();
        var forecast = input.Forecast;

        if (forecast.HasModel)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Chart,
                Title: "Evolución y proyección de la matrícula",
                Source: "Inscripciones registradas · proyección por mínimos cuadrados",
                Note: $"Pendiente de {Decimal(forecast.Slope)} alumnos por periodo, con R² de {Decimal(forecast.R2, 2)}.",
                Chart: new BoardReportChart(
                    "line",
                    forecast.Labels,
                    [
                        new("Matrícula", forecast.Actual, "teal"),
                        new("Tendencia", forecast.Trend, "indigo", Dashed: true),
                    ],
                    ForecastFromIndex: forecast.HistoricalCount)));
        }
        else
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Pending,
                Title: "Evolución de la matrícula",
                Text: "Se necesitan inscripciones registradas en al menos tres periodos para trazar la serie."));
        }

        if (input.Dashboard.StudentsByCareer.Count > 0)
        {
            var total = input.Dashboard.StudentsByCareer.Sum(c => c.Students);
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Alumnos activos por programa educativo",
                Source: "Registros del sistema",
                Table: new BoardReportTable(
                    ["Programa educativo", "Alumnos", "Participación %"],
                    [.. input.Dashboard.StudentsByCareer.Select(c => new BoardReportRow(
                        c.Career, [Count(c.Students), Percent(total == 0 ? 0 : 100.0 * c.Students / total)]))],
                    Total: [Count(total), "100"])));
        }

        var career = Capture(captures, "Carrera911_9A");
        blocks.Add(MatrixTable(career, "matricula_grado", "Matrícula reportada por grado",
            "Formato 911.9A · Carrera",
            note: Text(career, "nombre_carrera") is { } name ? $"Programa reportado: {name}." : null));

        blocks.Add(Narrative(input, "matricula.analisis", "Análisis de la matrícula",
            "La matrícula mantiene su tendencia de crecimiento. Se recomienda revisar la capacidad instalada "
            + "de los programas con mayor participación antes de la siguiente convocatoria."));

        return new BoardReportChapter("II", "Matrícula",
            "Comportamiento histórico, distribución por programa y proyección.", blocks);
    }

    private static BoardReportChapter Achievement(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var blocks = new List<BoardReportBlock>();
        var periods = input.History.Periods;

        if (periods.Count > 0)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Chart,
                Title: "Aprobación y reprobación por periodo",
                Source: "Kardex registrado",
                Chart: new BoardReportChart(
                    "bar",
                    [.. periods.Select(p => p.Label)],
                    [
                        new("Aprobación %", [.. periods.Select(p => Math.Round(p.PassRate, 1))], "teal"),
                        new("Reprobación %", [.. periods.Select(p => Math.Round(100 - p.PassRate, 1))], "ochre"),
                    ])));

            var latest = periods[^1];
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Figures,
                Title: $"Indicadores del periodo {latest.Label}",
                Source: "Kardex e inscripciones registradas",
                Figures:
                [
                    new("Aprobación", Percent(latest.PassRate), $"{Count(latest.Passed)} de {Count(latest.Passed + latest.Failed)} materias"),
                    new("Bajas", Percent(latest.WithdrawalRate), $"{Count(latest.Withdrawn)} alumnos"),
                    new("Egresados", Count(latest.Graduated), "en el periodo"),
                    new("Nuevo ingreso", Count(latest.NewEnrolled), "primera inscripción"),
                ]));
        }

        var students = Capture(captures, "SII_Alumnos");
        if (students is { IsSubmitted: true })
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Indicadores reportados al SII",
                Source: SourceOf(students),
                Note: "El área reporta estos índices por sexo; el sistema los calcula del kardex de forma independiente.",
                Table: new BoardReportTable(
                    ["Indicador", "Mujeres", "Hombres"],
                    [
                        SexRow(students, "aprovechamiento", "Calificación promedio", "Calificación promedio"),
                        SexRow(students, "eficiencia_terminal", "Eficiencia terminal (%)", "Eficiencia terminal"),
                        SexRow(students, "desercion", "Deserción (%)", "Deserción"),
                    ])));
        }
        else
        {
            blocks.Add(Pending("Indicadores reportados al SII", students, "Formato SII · Perfil del alumno"));
        }

        blocks.Add(Narrative(input, "aprovechamiento.analisis", "Análisis del aprovechamiento",
            "El índice de aprobación se mantiene dentro del rango histórico de la universidad. "
            + "Las academias darán seguimiento a las asignaturas con mayor reprobación."));

        return new BoardReportChapter("III", "Aprovechamiento académico",
            "Aprobación, reprobación y los índices que se reportan al SII.", blocks);
    }

    private static BoardReportChapter Graduation(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var students = Capture(captures, "SII_Alumnos");
        var career = Capture(captures, "Carrera911_9A");

        return new BoardReportChapter("IV", "Egreso y titulación",
            "Egresados, titulados y cédulas profesionales del ciclo.",
            [
                MatrixTable(students, "titulados", "Egreso y titulación", "Formato SII · Perfil del alumno"),
                MatrixTable(career, "egresados_titulados", "Egresados y titulados del programa", "Formato 911.9A · Carrera"),
                Narrative(input, "titulacion.analisis", "Análisis de la titulación",
                    "La diferencia entre egreso y titulación se concentra en el trámite de cédula profesional. "
                    + "Se propone reforzar el acompañamiento administrativo posterior al egreso."),
            ]);
    }

    private static BoardReportChapter Scholarships(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var students = Capture(captures, "SII_Alumnos");
        var school = Capture(captures, "Escuela911_9");

        return new BoardReportChapter("V", "Becas y apoyos",
            "Becas vigentes por programa y por origen del financiamiento.",
            [
                MatrixTable(students, "becas", "Becas por programa", "Formato SII · Perfil del alumno", onlyWithValues: true),
                MatrixTable(school, "becas_por_origen", "Becas por origen del financiamiento", "Formato 911.9 · Escuela"),
                Narrative(input, "becas.analisis", "Análisis de becas",
                    "La cobertura de becas sostiene la permanencia de los alumnos con menor ingreso familiar. "
                    + "Se recomienda cruzar el padrón con la lista de riesgo de deserción del capítulo X."),
            ]);
    }

    private static BoardReportChapter Staff(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var blocks = new List<BoardReportBlock>();

        if (input.Staff.Count > 0)
        {
            var groups = new (string Label, Func<BoardStaffMember, bool> Match)[]
            {
                ("Docentes", s => s.IsTeacher),
                ("Personal administrativo y de apoyo", s => !s.IsTeacher),
            };

            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Plantilla registrada por función",
                Source: "Registros del sistema · Recursos Humanos",
                Table: new BoardReportTable(
                    ["Función", "Hombres", "Mujeres", "Total"],
                    [.. groups.Select(g =>
                    {
                        var people = input.Staff.Where(g.Match).ToList();
                        return new BoardReportRow(g.Label,
                        [
                            Count(people.Count(p => p.Gender == "H")),
                            Count(people.Count(p => p.Gender == "M")),
                            Count(people.Count),
                        ]);
                    })],
                    Total:
                    [
                        Count(input.Staff.Count(p => p.Gender == "H")),
                        Count(input.Staff.Count(p => p.Gender == "M")),
                        Count(input.Staff.Count),
                    ])));
        }

        var school = Capture(captures, "Escuela911_9");
        blocks.Add(MatrixTable(school, "personal_funcion", "Personal por función reportado", "Formato 911.9 · Escuela", onlyWithValues: true));
        blocks.Add(MatrixTable(school, "docentes_nivel_estudios", "Docentes por nivel máximo de estudios", "Formato 911.9 · Escuela", onlyWithValues: true));

        blocks.Add(Narrative(input, "personal.analisis", "Análisis de la plantilla",
            $"La universidad cuenta con {Count(input.Dashboard.TeachersWithPostgrad)} docentes con estudios de posgrado, "
            + "condición que sostiene los procesos de acreditación de los programas educativos."));

        return new BoardReportChapter("VI", "Personal académico y administrativo",
            "Plantilla por función y nivel de estudios.", blocks);
    }

    private static BoardReportChapter Facilities(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var blocks = new List<BoardReportBlock>();

        if (input.Facilities.Count > 0)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Espacios registrados",
                Source: "Registros del sistema · Recursos Materiales",
                Table: new BoardReportTable(
                    ["Tipo de espacio", "Espacios", "Capacidad", "En uso"],
                    [.. input.Facilities
                        .GroupBy(f => f.Type)
                        .OrderByDescending(g => g.Count())
                        .Select(g => new BoardReportRow(g.Key,
                        [
                            Count(g.Count()),
                            Count(g.Sum(f => f.Capacity)),
                            Count(g.Count(f => f.InUse)),
                        ]))])));
        }

        var school = Capture(captures, "Escuela911_9");
        blocks.Add(MatrixTable(school, "espacios", "Espacios educativos reportados", "Formato 911.9 · Escuela"));

        var tic = Capture(captures, "AnexoTI");
        if (tic is { IsSubmitted: false })
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Pending,
                Title: "Equipamiento de cómputo",
                Text: $"El Anexo de Tecnologías de la Información sigue en borrador ({Date(tic.LastModified)}); "
                    + "el inventario de cómputo se integrará cuando se envíe."));
        }

        blocks.Add(Narrative(input, "infraestructura.analisis", "Análisis de la infraestructura",
            "Los espacios educativos cubren la demanda del ciclo. Las necesidades de mantenimiento se "
            + "atenderán con cargo al programa anual de conservación."));

        return new BoardReportChapter("VII", "Infraestructura y equipamiento",
            "Espacios educativos, capacidad instalada y equipamiento.", blocks);
    }

    private static BoardReportChapter Outreach(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var institution = Capture(captures, "Institucion911_10");

        return new BoardReportChapter("VIII", "Vinculación y extensión",
            "Servicio social, prácticas profesionales y proyectos con los sectores.",
            [
                MatrixTable(institution, "servicio_social", "Servicio social por sector", "Formato 911.10 · Institución"),
                MatrixTable(institution, "practicas_profesionales", "Prácticas profesionales por sector", "Formato 911.10 · Institución"),
                MatrixTable(institution, "proyectos_vinculacion", "Proyectos de vinculación por sector", "Formato 911.10 · Institución", onlyWithValues: true),
                Narrative(input, "vinculacion.analisis", "Análisis de la vinculación",
                    "Los convenios vigentes sostienen la colocación de alumnos en estadía. "
                    + "Se buscará ampliar la participación del sector social en el siguiente ciclo."),
            ]);
    }

    private static BoardReportChapter Finance(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var institution = Capture(captures, "Institucion911_10");
        var blocks = new List<BoardReportBlock>();

        if (institution is { IsSubmitted: true })
        {
            var sources = new (string Label, string Key)[]
            {
                ("Subsidio federal", "ing_federal"),
                ("Subsidio estatal", "ing_estatal"),
                ("Inscripciones y colegiaturas", "ing_inscripciones"),
                ("Servicios", "ing_servicios"),
                ("Otros ingresos", "ing_otros"),
            };
            var total = sources.Sum(s => institution.State.Number(s.Key));

            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Ingresos del ejercicio",
                Source: SourceOf(institution),
                Table: new BoardReportTable(
                    ["Origen", "Importe", "Participación %"],
                    [.. sources.Select(s =>
                    {
                        var amount = institution.State.Number(s.Key);
                        return new BoardReportRow(s.Label,
                            [Money(amount), Percent(total == 0 ? 0 : 100 * amount / total)]);
                    })],
                    Total: [Money(total), "100"])));
        }
        else
        {
            blocks.Add(Pending("Ingresos del ejercicio", institution, "Formato 911.10 · Institución"));
        }

        blocks.Add(Narrative(input, "finanzas.analisis", "Análisis financiero",
            "El subsidio público sigue siendo la principal fuente de ingreso de la universidad. "
            + "Los ingresos propios se destinan a mantenimiento y equipamiento."));

        return new BoardReportChapter("IX", "Finanzas institucionales",
            "Origen de los ingresos del ejercicio reportado.", blocks);
    }

    private static BoardReportChapter Outlook(BoardReportInputs input)
    {
        var blocks = new List<BoardReportBlock>();
        var forecast = input.Forecast;

        if (forecast is { HasModel: true, NextValue: { } next })
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Figures,
                Title: "Proyección de matrícula",
                Source: "Regresión lineal sobre los periodos registrados",
                Figures:
                [
                    new(forecast.NextLabel ?? "Siguiente periodo", Count((int)Math.Round(next)),
                        $"± {Decimal(forecast.NextMargin ?? 0, 0)} alumnos"),
                    new("Crecimiento por periodo", Decimal(forecast.Slope), "alumnos"),
                    new("Ajuste del modelo", Decimal(forecast.R2, 2), "R²"),
                ]));
        }

        if (input.Dropout is { HasModel: true } dropout)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Riesgo de deserción por programa",
                Source: "Árbol de decisión entrenado con las trayectorias terminadas",
                Note: $"El modelo detecta el {Percent(dropout.Quality.Recall * 100)} de las bajas en validación cruzada.",
                Table: new BoardReportTable(
                    ["Programa educativo", "Alumnos evaluados", "Por atender", "Riesgo alto"],
                    [.. dropout.ByCareer.Select(c => new BoardReportRow(c.Career,
                        [Count(c.Students), Count(c.AtRisk), Count(c.HighRisk)]))],
                    Total:
                    [
                        Count(dropout.ScoredStudents),
                        Count(dropout.AtRiskStudents),
                        Count(dropout.HighRiskStudents),
                    ])));
        }

        if (input.Anomalies.Count > 0)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Desviaciones detectadas",
                Source: "Detección estadística sobre las series institucionales",
                Table: new BoardReportTable(
                    ["Periodo", "Hallazgo"],
                    [.. input.Anomalies.Select(a => new BoardReportRow(a.Period ?? "—", [a.Title]))])));
        }

        blocks.Add(Narrative(input, "prospectiva.acuerdos", "Acuerdos propuestos a la Junta",
            "Se propone a la Junta Directiva aprobar el presente informe y dar seguimiento a las acciones "
            + "derivadas de los capítulos de matrícula, aprovechamiento y prospectiva."));

        return new BoardReportChapter("X", "Prospectiva y acuerdos",
            "Proyección, riesgos detectados y acuerdos propuestos.", blocks);
    }

    // ── Blocks ──────────────────────────────────────────────────────────────

    private static BoardReportBlock Narrative(BoardReportInputs input, string key, string title, string suggested)
    {
        var saved = input.Narratives.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;
        return new BoardReportBlock(
            BoardReportBlockKinds.Narrative,
            Title: title,
            Text: saved ?? suggested,
            BlockKey: key,
            IsSuggested: saved is null,
            Source: saved is null ? "Redacción sugerida; la sustituye el área responsable" : "Redactado por la universidad");
    }

    /// <summary>A matrix of an official format, with its own rows, columns and totals.</summary>
    private static BoardReportBlock MatrixTable(
        BoardReportCapture? capture, string questionKey, string title, string formatName,
        string? note = null, bool onlyWithValues = false)
    {
        if (capture is not { IsSubmitted: true }) return Pending(title, capture, formatName);

        var question = Question(capture, questionKey);
        if (question is null)
            return new BoardReportBlock(BoardReportBlockKinds.Pending, Title: title,
                Text: $"El formato «{formatName}» ya no incluye este reactivo.");

        var state = capture.State;
        var columns = question.Columns;
        var rows = new List<BoardReportRow>();

        foreach (var row in question.Rows)
        {
            var values = columns.Select(column => state.CellTotal(question, row, column)).ToList();
            if (onlyWithValues && values.All(v => Math.Abs(v) < 0.001)) continue;

            var cells = values.Select(Decimal).ToList();
            if (question.RowTotal) cells.Add(Decimal(state.RowTotal(question, row)));
            rows.Add(new BoardReportRow(row, cells));
        }

        if (rows.Count == 0)
            return new BoardReportBlock(BoardReportBlockKinds.Pending, Title: title,
                Text: $"El área envió «{formatName}» sin cifras en este apartado.", Source: SourceOf(capture));

        var header = columns.ToList();
        if (question.RowTotal) header.Add(question.RowTotalLabel ?? "Total");

        List<string>? total = null;
        if (question.ColumnTotal)
        {
            total = [.. columns.Select(column => Decimal(state.ColumnTotal(question, column)))];
            if (question.RowTotal) total.Add(Decimal(state.RowTotalsSum(question)));
        }

        return new BoardReportBlock(
            BoardReportBlockKinds.Table,
            Title: title,
            Source: SourceOf(capture),
            Note: note ?? (onlyWithValues && rows.Count < question.Rows.Count
                ? "Se muestran únicamente los renglones con cifras."
                : null),
            Table: new BoardReportTable(["Concepto", .. header], rows, total is null ? null : ["Total", .. total]));
    }

    private static BoardReportBlock Pending(string title, BoardReportCapture? capture, string formatName) =>
        new(BoardReportBlockKinds.Pending,
            Title: title,
            Text: capture is null
                ? $"Falta capturar el formato «{formatName}» del ciclo."
                : $"El formato «{formatName}» sigue en borrador ({Date(capture.LastModified)}); "
                  + "sus cifras se integrarán cuando el área lo envíe.");

    // ── Reading a capture ───────────────────────────────────────────────────

    private static BoardReportCapture? Capture(Dictionary<string, BoardReportCapture> captures, string reportType) =>
        captures.TryGetValue(reportType, out var capture) ? capture : null;

    private static ReportTemplateQuestion? Question(BoardReportCapture capture, string key) =>
        capture.State.Template.Sections.SelectMany(s => s.Questions).FirstOrDefault(q => q.Key == key);

    private static string? Text(BoardReportCapture? capture, string key)
    {
        if (capture is not { IsSubmitted: true }) return null;
        var value = capture.State.Get(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static BoardReportRow SexRow(BoardReportCapture capture, string questionKey, string row, string label)
    {
        var question = Question(capture, questionKey);
        if (question is null) return new BoardReportRow(label, ["—", "—"]);

        return new BoardReportRow(label,
        [
            Decimal(capture.State.CellTotal(question, row, "Mujeres")),
            Decimal(capture.State.CellTotal(question, row, "Hombres")),
        ]);
    }

    private static string SourceOf(BoardReportCapture capture) =>
        $"{capture.DisplayName} · enviado el {Date(capture.LastModified)}"
        + (capture.CapturedBy is null ? string.Empty : $" por {capture.CapturedBy}");

    // ── Formatting ──────────────────────────────────────────────────────────

    private static BoardReportRow Row(string label, string value) => new(label, [value]);

    private static string Count(int value) => value.ToString("N0", Mx);

    private static string Decimal(double value, int decimals = 1) =>
        Math.Abs(value - Math.Round(value)) < 0.001
            ? value.ToString("N0", Mx)
            : value.ToString("N" + decimals, Mx);

    private static string Percent(double value) => value.ToString("N1", Mx) + " %";

    private static string Money(double value) => "$" + value.ToString("N0", Mx);

    private static string Date(DateTime? value) =>
        value is null ? "sin fecha" : value.Value.ToLocalTime().ToString("dd/MM/yyyy", Mx);

    private static string? Join(string separator, params string?[] parts)
    {
        var present = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return present.Count == 0 ? null : string.Join(separator, present);
    }
}
