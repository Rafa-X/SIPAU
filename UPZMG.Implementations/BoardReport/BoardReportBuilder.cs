using System.Globalization;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Intelligence;
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
                new BoardReportSource(c.ReportType, c.DisplayName, c.Status, c.LastModified, c.CapturedBy, c.OtherCaptures))]);
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
                $"El presente informe da cuenta de la operación de {Text(institution, "nombre_institucion") ?? InstitutionName} durante el ciclo {input.Cycle}, "
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
                Source: "Inscripciones registradas · proyección de la tendencia",
                Note: $"{ForecastTrend.Sentence(forecast.Slope)}; {FitNote(forecast)}.",
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

        blocks.Add(Narrative(input, "matricula.analisis", "Análisis de la matrícula", EnrolmentAnalysis(input)));

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
            // Only periods with a kardex. A period nobody has graded has no pass rate, and
            // drawing its complement reported it as a period everybody failed.
            var graded = periods.Where(p => p.HasGrades).ToList();
            if (graded.Count > 0)
            {
                blocks.Add(new BoardReportBlock(
                    BoardReportBlockKinds.Chart,
                    Title: "Aprobación y reprobación por periodo",
                    Source: "Kardex registrado",
                    Note: graded.Count < periods.Count
                        ? "Se muestran los periodos con kardex capturado."
                        : null,
                    Chart: new BoardReportChart(
                        "bar",
                        [.. graded.Select(p => p.Label)],
                        [
                            new("Aprobación %", [.. graded.Select(p => Math.Round(p.PassRate!.Value, 1))], "teal"),
                            new("Reprobación %", [.. graded.Select(p => Math.Round(100 - p.PassRate!.Value, 1))], "ochre"),
                        ])));
            }

            var latest = periods[^1];
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Figures,
                Title: $"Indicadores del periodo {latest.Label}",
                Source: "Kardex e inscripciones registradas",
                Figures:
                [
                    new("Aprobación",
                        latest.PassRate is { } rate ? Percent(rate) : "Sin calificaciones",
                        $"{Count(latest.Passed)} de {Count(latest.Passed + latest.Failed)} oportunidades calificadas"),
                    new("Bajas", Percent(latest.WithdrawalRate), $"{Count(latest.Withdrawn)} alumnos"),
                    new("Egresados", Count(latest.Graduated), "en el periodo"),
                    new("Nuevo ingreso", Count(latest.NewEnrolled), "ingreso registrado"),
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

        blocks.Add(Narrative(input, "aprovechamiento.analisis", "Análisis del aprovechamiento", AchievementAnalysis(input)));

        return new BoardReportChapter("III", "Aprovechamiento académico",
            "Aprobación, reprobación y los índices que se reportan al SII.", blocks);
    }

    private static BoardReportChapter Graduation(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var students = Capture(captures, "SII_Alumnos");
        var career = Capture(captures, "Carrera911_9A");

        var blocks = new List<BoardReportBlock>();

        // What Control Escolar registered, which is where the rate actually comes from.
        if (input.Titulation is { Graduates: > 0 } titulation)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Figures,
                Title: "Titulación registrada",
                Source: "Registros del sistema · Control Escolar",
                Note: "La eficiencia de titulación es titulados entre egresados; ninguno de los dos se captura a mano.",
                Figures:
                [
                    new("Egresados", Count(titulation.Graduates), "con estatus de egresado"),
                    new("Titulados", Count(titulation.Titled), "procesos registrados"),
                    new("Eficiencia de titulación", Percent(titulation.TitulationRate), null),
                ]));

            if (titulation.ByModality.Count > 0)
            {
                blocks.Add(new BoardReportBlock(
                    BoardReportBlockKinds.Table,
                    Title: "Titulados por modalidad",
                    Source: "Registros del sistema · Control Escolar",
                    Table: new BoardReportTable(
                        ["Modalidad", "Titulados"],
                        [.. titulation.ByModality.Select(m => new BoardReportRow(m.Modality, [Count(m.Count)]))],
                        Total: [Count(titulation.Titled)])));
            }
        }

        blocks.Add(MatrixTable(students, "titulados", "Egreso y titulación", "Formato SII · Perfil del alumno"));

        return new BoardReportChapter("IV", "Egreso y titulación",
            "Egresados, titulados y cédulas profesionales del ciclo.",
            [
                .. blocks,
                MatrixTable(career, "egresados_titulados", "Egresados y titulados del programa", "Formato 911.9A · Carrera"),
                Narrative(input, "titulacion.analisis", "Análisis de la titulación", GraduationAnalysis(input)),
            ]);
    }

    private static BoardReportChapter Scholarships(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var students = Capture(captures, "SII_Alumnos");
        var school = Capture(captures, "Escuela911_9");

        var blocks = new List<BoardReportBlock>();

        // The grant register, which is what the format's totals are supposed to summarise.
        if (input.Scholarships is { } scholarships && scholarships.Sources.Count > 0)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Becas registradas por origen del financiamiento",
                Source: $"Registros del sistema · Servicios Estudiantiles · {scholarships.Scope}",
                Note: "Un alumno con apoyos de dos orígenes se cuenta una vez en el total de becados.",
                Table: new BoardReportTable(
                    ["Origen", "Alumnos becados", "Monto"],
                    [.. scholarships.Sources.Select((source, i) => new BoardReportRow(source,
                    [
                        Count(scholarships.Students[i]),
                        Money((double)scholarships.Amounts[i]),
                    ]))],
                    Total: [Count(scholarships.Beneficiaries), Money((double)scholarships.TotalAmount)])));
        }

        blocks.Add(MatrixTable(students, "becas", "Becas por programa", "Formato SII · Perfil del alumno", onlyWithValues: true));

        return new BoardReportChapter("V", "Becas y apoyos",
            "Becas vigentes por programa y por origen del financiamiento.",
            [
                .. blocks,
                MatrixTable(school, "becas_por_origen", "Becas por origen del financiamiento", "Formato 911.9 · Escuela"),
                Narrative(input, "becas.analisis", "Análisis de becas", ScholarshipsAnalysis(input)),
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

        blocks.Add(Narrative(input, "personal.analisis", "Análisis de la plantilla", StaffAnalysis(input)));

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

        blocks.Add(Narrative(input, "infraestructura.analisis", "Análisis de la infraestructura", FacilitiesAnalysis(input)));

        return new BoardReportChapter("VII", "Infraestructura y equipamiento",
            "Espacios educativos, capacidad instalada y equipamiento.", blocks);
    }

    private static BoardReportChapter Outreach(
        BoardReportInputs input, Dictionary<string, BoardReportCapture> captures)
    {
        var institution = Capture(captures, "Institucion911_10");

        var blocks = new List<BoardReportBlock>();

        // What the area registered project by project, so the counts can be traced back.
        if (input.Outreach.Count > 0)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Figures,
                Title: "Proyectos registrados",
                Source: "Registros del sistema · Vinculación",
                Figures:
                [
                    new("Proyectos", Count(input.Outreach.Count), "registrados"),
                    new("Organizaciones", Count(input.Outreach.Select(p => p.Organization)
                        .Distinct(StringComparer.CurrentCultureIgnoreCase).Count()), "distintas"),
                    new("Horas acreditadas", Count(input.Outreach.Sum(p => p.AccreditedHours)), "en total"),
                    new("Con alumno asignado", Count(input.Outreach.Count(p => p.HasStudent)), "proyectos"),
                ]));

            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Proyectos por sector y actividad",
                Source: "Registros del sistema · Vinculación",
                Table: new BoardReportTable(
                    ["Sector", "Proyectos", "Horas acreditadas", "Con alumno"],
                    [.. input.Outreach
                        .GroupBy(p => p.Sector)
                        .OrderByDescending(g => g.Count())
                        .Select(g => new BoardReportRow(g.Key,
                        [
                            Count(g.Count()),
                            Count(g.Sum(p => p.AccreditedHours)),
                            Count(g.Count(p => p.HasStudent)),
                        ]))],
                    Total:
                    [
                        Count(input.Outreach.Count),
                        Count(input.Outreach.Sum(p => p.AccreditedHours)),
                        Count(input.Outreach.Count(p => p.HasStudent)),
                    ])));
        }

        blocks.Add(MatrixTable(institution, "servicio_social", "Servicio social por sector", "Formato 911.10 · Institución"));
        blocks.Add(MatrixTable(institution, "practicas_profesionales", "Prácticas profesionales por sector", "Formato 911.10 · Institución"));
        blocks.Add(MatrixTable(institution, "proyectos_vinculacion", "Proyectos de vinculación por sector", "Formato 911.10 · Institución", onlyWithValues: true));
        blocks.Add(Narrative(input, "vinculacion.analisis", "Análisis de la vinculación", OutreachAnalysis(input)));

        return new BoardReportChapter("VIII", "Vinculación y extensión",
            "Servicio social, prácticas profesionales y proyectos con los sectores.", blocks);
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

        // The administration's own summary: income and expenditure of the closed fiscal year.
        if (input.Finance is { } finance)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: $"Ingresos y egresos del ejercicio {finance.FiscalYear}",
                Source: "Registros del sistema · Administración y Finanzas",
                Table: new BoardReportTable(
                    ["Concepto", "Importe"],
                    [
                        new BoardReportRow("Subsidio federal", [Money((double)finance.FederalIncome)]),
                        new BoardReportRow("Subsidio estatal", [Money((double)finance.StateIncome)]),
                        new BoardReportRow("Ingresos propios: inscripciones", [Money((double)finance.OwnIncomeEnrollment)]),
                        new BoardReportRow("Ingresos propios: servicios", [Money((double)finance.OwnIncomeServices)]),
                        new BoardReportRow("Total de ingresos", [Money((double)finance.TotalIncome)]),
                        new BoardReportRow("Nómina", [Money((double)finance.PayrollExpense)]),
                        new BoardReportRow("Materiales", [Money((double)finance.MaterialsExpense)]),
                        new BoardReportRow("Servicios", [Money((double)finance.ServicesExpense)]),
                        new BoardReportRow("Inversión", [Money((double)finance.InvestmentExpense)]),
                        new BoardReportRow("Total de egresos", [Money((double)finance.TotalExpense)]),
                    ],
                    Total: [Money((double)(finance.TotalIncome - finance.TotalExpense))]),
                Note: "El renglón final es el balance del ejercicio: ingresos menos egresos."));
        }

        blocks.Add(Narrative(input, "finanzas.analisis", "Análisis financiero", FinanceAnalysis(input)));

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
                Source: "Tendencia de los periodos registrados",
                Figures:
                [
                    new(forecast.NextLabel ?? "Siguiente periodo", Count((int)Math.Round(next)),
                        forecast.NextMargin is { } margin ? $"± {Decimal(margin, 0)} alumnos" : "sin margen medible"),
                    new("Cambio por periodo", Decimal(forecast.Slope), "alumnos"),
                    new("Lo que explica la tendencia", forecast.R2 is { } r2 ? Percent(r2 * 100) : "—", "de los cambios de matrícula"),
                ]));
        }

        if (input.Dropout is { HasModel: true } dropout)
        {
            blocks.Add(new BoardReportBlock(
                BoardReportBlockKinds.Table,
                Title: "Riesgo de deserción por programa",
                Source: "Modelo de riesgo, que aprende de los alumnos que ya terminaron",
                Note: $"En pruebas con alumnos que ya terminaron detectó {Percent(dropout.Quality.Recall * 100)} de las bajas.",
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
                Title: "Alertas de indicadores",
                Source: "Revisión de los indicadores institucionales",
                Table: new BoardReportTable(
                    ["Periodo", "Hallazgo"],
                    [.. input.Anomalies.Select(a => new BoardReportRow(a.Period ?? "—", [$"{a.Title}. {a.Detail}"]))])));
        }

        blocks.Add(Narrative(input, "prospectiva.acuerdos", "Acuerdos propuestos a la Junta",
            "Se propone a la Junta Directiva aprobar el presente informe y dar seguimiento a las acciones "
            + "derivadas de los capítulos de matrícula, aprovechamiento y prospectiva."));

        return new BoardReportChapter("X", "Prospectiva y acuerdos",
            "Proyección, riesgos detectados y acuerdos propuestos.", blocks);
    }

    // ── Suggested texts ─────────────────────────────────────────────────────
    //
    // Each one is written from the figures of this report. They used to be fixed sentences ("la
    // matrícula mantiene su tendencia de crecimiento"), which stayed on the page of a university
    // whose enrolment was falling, one click away from being sent to the board as its own words.

    private const double Noticeable = 2;

    private static string EnrolmentAnalysis(BoardReportInputs input)
    {
        var periods = input.History.Periods.Where(p => p.Enrolled > 0).ToList();
        if (periods.Count < 2)
            return "Todavía no hay inscripciones registradas en dos periodos para comentar la evolución de la matrícula.";

        var first = periods[0];
        var last = periods[^1];
        var change = 100.0 * (last.Enrolled - first.Enrolled) / first.Enrolled;

        var text = $"La matrícula pasó de {Count(first.Enrolled)} alumnos en {first.Label} a {Count(last.Enrolled)} en {last.Label} "
            + $"({change.ToString("+0.0;−0.0;0.0", Mx)} %).";

        if (input.Forecast is { HasModel: true, NextValue: { } next })
            text += $" La tendencia proyecta {Count((int)Math.Round(next))} alumnos para {input.Forecast.NextLabel}.";

        var careers = input.Dashboard.StudentsByCareer;
        if (careers.Count > 1 && careers.Sum(c => c.Students) is > 0 and var total)
        {
            var top = careers.MaxBy(c => c.Students)!;
            text += $" {top.Career} concentra {Percent(100.0 * top.Students / total)} de los alumnos activos.";
        }

        return change switch
        {
            >= Noticeable => text + " Conviene revisar la capacidad instalada de los programas con mayor participación antes de la siguiente convocatoria.",
            <= -Noticeable => text + " Conviene revisar con los programas las causas de la disminución antes de la siguiente convocatoria.",
            _ => text,
        };
    }

    private static string AchievementAnalysis(BoardReportInputs input)
    {
        var graded = input.History.Periods.Where(p => p.HasGrades).ToList();
        if (graded.Count == 0) return "Todavía no hay calificaciones capturadas para comentar el aprovechamiento.";

        var latest = graded[^1];
        var rate = latest.PassRate!.Value;
        var text = $"En {latest.Label} se aprobó {Percent(rate)} de las oportunidades calificadas";
        if (graded.Count == 1) return text + ".";

        var mean = graded.SkipLast(1).Average(p => p.PassRate!.Value);
        var gap = rate - mean;
        var compared = Math.Abs(gap) < Noticeable ? "cerca del" : gap > 0 ? "por encima del" : "por debajo del";
        text += $", {compared} promedio de los periodos anteriores ({Percent(mean)}).";

        return gap <= -Noticeable ? text + " Conviene dar seguimiento a las asignaturas con mayor reprobación." : text;
    }

    private static string GraduationAnalysis(BoardReportInputs input)
    {
        if (input.Titulation is not { Graduates: > 0 } titulation)
            return "Todavía no hay egresados registrados para comentar la titulación.";

        var text = (titulation.Graduates == 1 ? "De 1 egresado, " : $"De {Count(titulation.Graduates)} egresados, ")
            + (titulation.Titled == 1 ? "1 tiene" : $"{Count(titulation.Titled)} tienen")
            + $" un proceso de titulación registrado ({Percent(titulation.TitulationRate)}).";

        if (titulation.ByModality.Count > 0)
            text += $" La modalidad más frecuente es {titulation.ByModality.MaxBy(m => m.Count)!.Modality}.";

        return titulation.TitulationRate < 50
            ? text + " Conviene reforzar el acompañamiento administrativo posterior al egreso."
            : text;
    }

    private static string ScholarshipsAnalysis(BoardReportInputs input)
    {
        if (input.Scholarships is not { Beneficiaries: > 0 } scholarships)
            return "Todavía no hay becas registradas para comentar.";

        var text = (scholarships.Beneficiaries == 1 ? "1 alumno tiene" : $"{Count(scholarships.Beneficiaries)} alumnos tienen")
            + $" beca en {scholarships.Scope}, por un total de {Money((double)scholarships.TotalAmount)}.";

        var main = Enumerable.Range(0, scholarships.Sources.Count).MaxBy(i => scholarships.Amounts[i]);
        if (scholarships.Sources.Count > 1) text += $" La fuente con mayor monto es {scholarships.Sources[main]}.";

        return input.Dropout is { HasModel: true }
            ? text + " Se recomienda cruzar el padrón con la lista de riesgo de deserción del capítulo X."
            : text;
    }

    private static string StaffAnalysis(BoardReportInputs input)
    {
        var teachers = input.Dashboard.TotalTeachers;
        if (teachers == 0) return "Todavía no hay docentes registrados para comentar la plantilla.";

        var postgrad = input.Dashboard.TeachersWithPostgrad;
        return $"La universidad tiene {Plural(teachers, "docente registrado", "docentes registrados")}, {Count(postgrad)} con estudios de posgrado "
            + $"({Percent(100.0 * postgrad / teachers)}).";
    }

    private static string FacilitiesAnalysis(BoardReportInputs input)
    {
        if (input.Facilities.Count == 0) return "Todavía no hay espacios registrados para comentar la infraestructura.";

        var idle = input.Facilities.Count(f => !f.InUse);
        var count = input.Facilities.Count;
        var text = $"Hay {Plural(count, "espacio registrado", "espacios registrados")}, con capacidad para {Count(input.Facilities.Sum(f => f.Capacity))} personas.";

        return idle switch
        {
            0 => text + (count == 1 ? " Está en uso." : " Todos están en uso."),
            1 => text + (count == 1 ? " Está fuera de uso; conviene revisar si requiere mantenimiento." : " Uno está fuera de uso; conviene revisar si requiere mantenimiento."),
            _ => text + $" {Count(idle)} están fuera de uso; conviene revisar si requieren mantenimiento.",
        };
    }

    private static string OutreachAnalysis(BoardReportInputs input)
    {
        if (input.Outreach.Count == 0) return "Todavía no hay proyectos de vinculación registrados para comentar.";

        var organizations = input.Outreach.Select(p => p.Organization).Distinct(StringComparer.CurrentCultureIgnoreCase).Count();
        var withStudent = input.Outreach.Count(p => p.HasStudent);
        return (input.Outreach.Count == 1 ? "Se registró 1 proyecto" : $"Se registraron {Count(input.Outreach.Count)} proyectos")
            + $" con {Plural(organizations, "organización", "organizaciones")}, con "
            + $"{Plural(input.Outreach.Sum(p => p.AccreditedHours), "hora acreditada", "horas acreditadas")}; "
            + (withStudent == 1 ? "1 tiene" : $"{Count(withStudent)} tienen") + " un alumno asignado.";
    }

    private static string FinanceAnalysis(BoardReportInputs input)
    {
        if (input.Finance is not { } finance) return "Todavía no hay un resumen financiero capturado para comentar.";

        var balance = finance.TotalIncome - finance.TotalExpense;
        var text = $"En el ejercicio {finance.FiscalYear} los ingresos fueron de {Money((double)finance.TotalIncome)} "
            + $"y los egresos de {Money((double)finance.TotalExpense)}, ";

        text += balance switch
        {
            > 0 => $"con un superávit de {Money((double)balance)}.",
            < 0 => $"con un déficit de {Money((double)-balance)}.",
            _ => "sin diferencia entre ambos.",
        };

        return finance.TotalIncome > 0
            ? text + $" El subsidio público aportó {Percent((double)(100 * (finance.FederalIncome + finance.StateIncome) / finance.TotalIncome))} de los ingresos."
            : text;
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

    private static string Plural(int value, string one, string many) => $"{Count(value)} {(value == 1 ? one : many)}";

    // Two observed periods fit any line exactly, so the report says so instead of printing R² = 1.
    private static string FitNote(EnrollmentForecast forecast) =>
        forecast.R2 is { } r2
            ? $"la tendencia explica {Percent(r2 * 100)} de los cambios"
            : "tendencia preliminar: con dos periodos observados todavía no se puede medir qué tan firme es";

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
