using UPZMG.Implementations.BoardReport;
using UPZMG.Implementations.Dashboard;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Intelligence;
using Xunit;

namespace UPZMG.Tests.Hardening;

/// <summary>
/// The texts the board report suggests are written from its figures. They used to be fixed
/// sentences, so "la matrícula mantiene su tendencia de crecimiento" also appeared for a university
/// whose enrolment was falling, one click away from reaching the board as its own words.
/// </summary>
public class BoardReportSuggestionsTests
{
    private static PeriodStats Period(int index, int enrolled, double? passRate = 80, int withdrawn = 0)
    {
        var passed = passRate is { } rate ? (int)Math.Round(rate * 10) : 0;
        return new PeriodStats(Guid.NewGuid(), $"{2024 + index / 3} Periodo{index % 3 + 1}", new DateOnly(2024, 1, 1).AddMonths(4 * index),
            enrolled, 0, withdrawn, 0, passed, passRate is null ? 0 : 1000 - passed);
    }

    private static BoardReportInputs Inputs(
        IReadOnlyList<PeriodStats>? periods = null,
        DashboardSummary? dashboard = null,
        IReadOnlyList<BoardFacility>? facilities = null,
        IReadOnlyList<BoardOutreachProject>? outreach = null,
        ScholarshipsBySource? scholarships = null,
        TitulationSummary? titulation = null,
        BoardFinanceSummary? finance = null,
        Dictionary<string, string>? narratives = null) =>
        new("2024-2025", dashboard ?? new DashboardSummary(), new AcademicHistory(periods ?? [], []), EnrollmentForecast.None,
            null, [], [], facilities ?? [], outreach ?? [], scholarships, titulation, finance, [],
            narratives ?? new Dictionary<string, string>(), "draft", null);

    private static BoardReportBlock Block(BoardReportInputs input, string key) =>
        BoardReportBuilder.Build(input, DateTime.UtcNow).Chapters.SelectMany(c => c.Blocks).Single(b => b.BlockKey == key);

    private static string Suggested(BoardReportInputs input, string key) => Block(input, key).Text!;

    [Fact]
    public void Enrolment_ThatGrowsSaysSoAndLooksAtCapacity()
    {
        var text = Suggested(Inputs([Period(0, 500), Period(1, 520), Period(2, 560)]), "matricula.analisis");

        Assert.Contains("pasó de 500 alumnos", text);
        Assert.Contains("a 560", text);
        Assert.Contains("+12.0 %", text);
        Assert.Contains("capacidad instalada", text);
        Assert.DoesNotContain("disminución", text);
    }

    [Fact]
    public void Enrolment_ThatFallsNeverClaimsGrowth()
    {
        var text = Suggested(Inputs([Period(0, 560), Period(1, 520), Period(2, 500)]), "matricula.analisis");

        Assert.Contains("pasó de 560 alumnos", text);
        Assert.Contains("−10.7 %", text);
        Assert.Contains("causas de la disminución", text);
        Assert.DoesNotContain("crecimiento", text);
        Assert.DoesNotContain("capacidad instalada", text);
    }

    [Fact]
    public void Enrolment_ThatHoldsRecommendsNothing()
    {
        var text = Suggested(Inputs([Period(0, 500), Period(1, 501)]), "matricula.analisis");

        Assert.DoesNotContain("Conviene", text);
    }

    [Fact]
    public void Enrolment_WithoutHistoryDoesNotInventOne() =>
        Assert.StartsWith("Todavía no hay inscripciones", Suggested(Inputs([Period(0, 500)]), "matricula.analisis"));

    [Fact]
    public void Enrolment_NamesTheLargestProgramme()
    {
        var dashboard = new DashboardSummary { StudentsByCareer = [new("A", "Sistemas", 75), new("B", "Mecatrónica", 25)] };

        Assert.Contains("Sistemas concentra 75.0 %", Suggested(Inputs([Period(0, 500), Period(1, 500)], dashboard), "matricula.analisis"));
    }

    [Theory]
    [InlineData(70, "por debajo del", true)]
    [InlineData(90, "por encima del", false)]
    [InlineData(80.5, "cerca del", false)]
    public void Achievement_ComparesWithItsOwnHistory(double latest, string expected, bool asksForFollowUp)
    {
        var text = Suggested(Inputs([Period(0, 500, 80), Period(1, 500, 80), Period(2, 500, latest)]), "aprovechamiento.analisis");

        Assert.Contains(expected, text);
        Assert.Contains("80.0 %", text);
        Assert.Equal(asksForFollowUp, text.Contains("seguimiento"));
    }

    [Fact]
    public void Achievement_IgnoresPeriodsWithoutGrades()
    {
        var text = Suggested(Inputs([Period(0, 500, 80), Period(1, 500, null)]), "aprovechamiento.analisis");

        Assert.Contains("2024 Periodo1", text);
        Assert.DoesNotContain("promedio", text);
    }

    [Fact]
    public void Achievement_WithoutGradesSaysSo() =>
        Assert.StartsWith("Todavía no hay calificaciones", Suggested(Inputs([Period(0, 500, null)]), "aprovechamiento.analisis"));

    [Fact]
    public void Graduation_ReadsItsFigures()
    {
        var low = new TitulationSummary(100, 20, [new("Tesis", 5), new("Estadía", 15)]);
        var text = Suggested(Inputs(titulation: low), "titulacion.analisis");

        Assert.Contains("De 100 egresados, 20", text);
        Assert.Contains("20.0 %", text);
        Assert.Contains("modalidad más frecuente es Estadía", text);
        Assert.Contains("acompañamiento", text);

        Assert.DoesNotContain("acompañamiento", Suggested(Inputs(titulation: new TitulationSummary(100, 90, [])), "titulacion.analisis"));
        Assert.StartsWith("Todavía no hay egresados", Suggested(Inputs(), "titulacion.analisis"));
    }

    [Fact]
    public void Scholarships_NameTheLargestSourceAndAvoidInventedClaims()
    {
        var grants = new ScholarshipsBySource(["Federal", "Estatal"], [10, 30], [1000m, 9000m], "2026 Ene-Abr", 35);
        var text = Suggested(Inputs(scholarships: grants), "becas.analisis");

        Assert.Contains("35 alumnos tienen beca en 2026 Ene-Abr", text);
        Assert.Contains("$10,000", text);
        Assert.Contains("mayor monto es Estatal", text);
        Assert.DoesNotContain("permanencia", text);
        Assert.StartsWith("Todavía no hay becas", Suggested(Inputs(), "becas.analisis"));
    }

    [Fact]
    public void Staff_ReadsItsFigures()
    {
        var dashboard = new DashboardSummary { TotalTeachers = 8, TeachersWithPostgrad = 2 };

        Assert.Contains("8 docentes registrados, 2 con estudios de posgrado (25.0 %)", Suggested(Inputs(dashboard: dashboard), "personal.analisis"));
        Assert.StartsWith("Todavía no hay docentes", Suggested(Inputs(), "personal.analisis"));
    }

    [Fact]
    public void Facilities_SayWhatIsOutOfUse()
    {
        BoardFacility Room(bool inUse) => new("Aula", "Aula", 30, inUse);

        Assert.Contains("Todos están en uso", Suggested(Inputs(facilities: [Room(true), Room(true)]), "infraestructura.analisis"));
        Assert.Contains("Uno está fuera de uso", Suggested(Inputs(facilities: [Room(true), Room(false)]), "infraestructura.analisis"));
        Assert.Contains("Hay 1 espacio registrado", Suggested(Inputs(facilities: [Room(false)]), "infraestructura.analisis"));
        Assert.Contains("2 están fuera de uso", Suggested(Inputs(facilities: [Room(false), Room(false)]), "infraestructura.analisis"));
        Assert.Contains("capacidad para 60 personas", Suggested(Inputs(facilities: [Room(false), Room(false)]), "infraestructura.analisis"));
        Assert.StartsWith("Todavía no hay espacios", Suggested(Inputs(), "infraestructura.analisis"));
    }

    [Fact]
    public void Outreach_ReadsItsFigures()
    {
        BoardOutreachProject[] projects = [new("Privado", "Estadía", "ACME", 480, true), new("Social", "Estadía", "acme", 100, false)];
        var text = Suggested(Inputs(outreach: projects), "vinculacion.analisis");

        Assert.Contains("2 proyectos con 1 organización", text);
        Assert.Contains("580 horas", text);
        Assert.Contains("1 tiene un alumno", text);
    }

    [Theory]
    [InlineData(100, 80, "superávit de $20")]
    [InlineData(80, 100, "déficit de $20")]
    [InlineData(100, 100, "sin diferencia")]
    public void Finance_SaysWhatTheBalanceIs(decimal income, decimal expense, string expected)
    {
        var finance = new BoardFinanceSummary(2025, income, 0, 0, 0, expense, 0, 0, 0);

        Assert.Contains(expected, Suggested(Inputs(finance: finance), "finanzas.analisis"));
    }

    [Fact]
    public void Finance_WithoutIncomeDoesNotDivideByZero()
    {
        var text = Suggested(Inputs(finance: new BoardFinanceSummary(2025, 0, 0, 0, 0, 0, 0, 0, 0)), "finanzas.analisis");

        Assert.DoesNotContain("NaN", text);
        Assert.DoesNotContain("subsidio", text);
    }

    [Fact]
    public void WhatTheUniversityWroteIsNeverReplaced()
    {
        var block = Block(Inputs([Period(0, 560), Period(1, 500)], narratives: new() { ["matricula.analisis"] = "Texto de la rectoría." }), "matricula.analisis");

        Assert.Equal("Texto de la rectoría.", block.Text);
        Assert.False(block.IsSuggested);
    }

    [Fact]
    public void NoSuggestionOfAnEmptyReportStatesAFact()
    {
        var chapters = BoardReportBuilder.Build(Inputs(), DateTime.UtcNow).Chapters;
        var suggestions = chapters.SelectMany(c => c.Blocks)
            .Where(b => b.BlockKey is not null && b.BlockKey.EndsWith(".analisis"))
            .Select(b => b.Text!)
            .ToList();

        Assert.NotEmpty(suggestions);
        Assert.All(suggestions, text => Assert.StartsWith("Todavía no hay", text));
    }

    [Theory]
    [InlineData(6.34, "crece cerca de 6.3 alumnos")]
    [InlineData(-6.34, "baja cerca de 6.3 alumnos")]
    [InlineData(1.0, "crece cerca de 1 alumno por")]
    [InlineData(0.01, "se mantiene estable")]
    public void TheTrendIsSaidTheWayItGoes(double slope, string expected) =>
        Assert.Contains(expected, ForecastTrend.Sentence(slope));
}
