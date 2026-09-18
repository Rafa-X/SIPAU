using System.Text.Json;
using UPZMG.Implementations.Intelligence;
using Xunit;

namespace UPZMG.Tests.Intelligence;

// The business rules on top of the maths: minimums, thresholds, scoring and alerts.
public class IntelligenceEngineTests
{
    private static readonly DateTime TrainedAt = new(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

    private static StudentObservation Observation(double average, double failureRate, bool works, bool? outcome, int n = 0) =>
        new(Guid.NewGuid(), $"M{n:0000}", $"Alumno {n}", n % 2 == 0 ? "Ingeniería Industrial" : "Ingeniería en Biotecnología",
            PeriodsObserved: 1, average, failureRate, Retakes: failureRate > 30 ? 2 : 0,
            EntryAge: 18 + n % 4, works, outcome);

    // A cohort where low averages leave and high averages finish, plus active students of both kinds.
    private static List<StudentObservation> Cohort(int finishedPerClass = 40, int activePerClass = 10)
    {
        var list = new List<StudentObservation>();
        int n = 0;
        for (int i = 0; i < finishedPerClass; i++) list.Add(Observation(5.5 + i % 10 / 10.0, 45, i % 2 == 0, true, n++));
        for (int i = 0; i < finishedPerClass; i++) list.Add(Observation(8.5 + i % 10 / 10.0, 5, i % 3 == 0, false, n++));
        for (int i = 0; i < activePerClass; i++) list.Add(Observation(5.6, 50, true, null, n++));
        for (int i = 0; i < activePerClass; i++) list.Add(Observation(9.2, 0, false, null, n++));
        return list;
    }

    private static AcademicHistory History(params int[] enrolled) =>
        History(enrolled.Select(e => (e, 80.0, 0)).ToArray());

    private static AcademicHistory History((int Enrolled, double PassRate, int Withdrawn)[] periods, params string[] upcoming)
    {
        string[] terms = ["Ene-Abr", "May-Ago", "Sep-Dic"];
        var stats = periods.Select((p, i) =>
        {
            var passed = (int)Math.Round(p.PassRate * 10);
            return new PeriodStats(Guid.NewGuid(), $"{2020 + i / 3} {terms[i % 3]}", new DateOnly(2020 + i / 3, 1 + 4 * (i % 3), 1),
                p.Enrolled, 0, p.Withdrawn, 0, passed, 1000 - passed);
        }).ToList();
        return new AcademicHistory(stats, upcoming);
    }

    // ── Levels ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "Bajo")]
    [InlineData(34.9, "Bajo")]
    [InlineData(35, "Medio")]
    [InlineData(49.9, "Medio")]
    [InlineData(50, "Alto")]
    [InlineData(100, "Alto")]
    public void LevelFor_UsesTheDecisionThresholdAndTheHighMark(double risk, string expected)
    {
        Assert.Equal(expected, IntelligenceEngine.LevelFor(risk));
    }

    [Theory]
    [InlineData("Trabaja", 0.5, null, "Trabaja")]
    [InlineData("Trabaja", null, 0.5, "No trabaja")]
    [InlineData("Edad al ingreso", null, 18.5, "Ingresó con 18 años o menos")]
    [InlineData("Edad al ingreso", 18.5, null, "Ingresó con 19 años o más")]
    [InlineData("Edad al ingreso", 18.5, 20.5, "Ingresó con 19 a 20 años")]
    [InlineData("Edad al ingreso", 18.5, 19.5, "Ingresó con 19 años")]
    [InlineData("Reprobación %", 22.65, null, "Reprobación mayor a 22.65 %")]
    [InlineData("Reprobación %", null, 40.0, "Reprobación de 40 % o menos")]
    [InlineData("Promedio", 5.92, 6.41, "Promedio entre 5.92 y 6.41")]
    [InlineData("Extraordinarios por periodo", null, 2.5, "Extraordinarios por periodo de 2.5 o menos")]
    public void Features_ReadAsPlainSpanish(string feature, double? above, double? atMost, string expected)
    {
        Assert.Equal(expected, DropoutFeatures.Describe(feature, above, atMost));
    }

    // ── Dropout model ───────────────────────────────────────────────────────

    [Fact]
    public void AnalyseDropout_RefusesWithTooFewFinishedTrajectories()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(Cohort(finishedPerClass: 9), unscoredActive: 3, TrainedAt);

        Assert.False(analysis.Report.HasModel);
        Assert.Contains("al menos 20", analysis.Report.Status);
        Assert.Empty(analysis.Students);
        // Nobody could be scored without a model.
        Assert.Equal(3 + 20, analysis.Report.UnscoredActiveStudents);
    }

    [Fact]
    public void AnalyseDropout_RefusesWhenAClassIsTooSmallToValidate()
    {
        var observations = Cohort().Where(o => o.Outcome != true).Concat(Cohort(finishedPerClass: 4).Where(o => o.Outcome == true)).ToList();

        var report = IntelligenceEngine.AnalyseDropout(observations, 0, TrainedAt).Report;

        Assert.False(report.HasModel);
        Assert.Equal(4, report.Withdrawals);
        Assert.Contains("bajas y", report.Status);
    }

    [Fact]
    public void AnalyseDropout_LearnsTheSignalAndScoresOnlyActiveStudents()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(Cohort(), unscoredActive: 2, TrainedAt);
        var report = analysis.Report;

        Assert.True(report.HasModel);
        Assert.Equal(80, report.FinishedTrajectories);
        Assert.Equal(40, report.Withdrawals);
        Assert.Equal(40, report.Graduates);
        Assert.Equal(IntelligenceEngine.Folds, report.Quality.Folds);
        Assert.Equal(80, report.Quality.Evaluated);
        Assert.True(report.Quality.Recall > 0.9, $"sensibilidad {report.Quality.Recall:P0}");
        Assert.Equal(TrainedAt, report.TrainedAtUtc);
        Assert.Equal(2, report.UnscoredActiveStudents);
        Assert.NotNull(report.Tree);

        Assert.Equal(20, analysis.Students.Count);
        Assert.Equal(20, report.ScoredStudents);
        Assert.Equal(10, report.AtRiskStudents);
        Assert.All(analysis.Students.Take(10), s => Assert.Equal("Alto", s.Level));
        Assert.All(analysis.Students.Skip(10), s => Assert.Equal("Bajo", s.Level));
        Assert.Equal(analysis.Students.Select(s => s.RiskPct).OrderByDescending(r => r), analysis.Students.Select(s => s.RiskPct));
        Assert.All(analysis.Students, s => Assert.NotEmpty(s.Reasons));
        Assert.True(report.TopStudents.Count <= 10);
        Assert.All(report.TopStudents, s => Assert.NotEqual("Bajo", s.Level));
    }

    [Fact]
    public void AnalyseDropout_SummarisesEachCareer()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(Cohort(), 0, TrainedAt);

        Assert.Equal(analysis.Students.Count, analysis.Report.ByCareer.Sum(c => c.Students));
        foreach (var career in analysis.Report.ByCareer)
        {
            var students = analysis.Students.Where(s => s.Career == career.Career).ToList();
            Assert.Equal(students.Count(s => s.Level != "Bajo"), career.AtRisk);
            Assert.Equal(Math.Round((double)career.AtRisk / career.Students * 100, 1), career.AtRiskPct);
            Assert.Equal(Math.Round(students.Average(s => s.RiskPct), 1), career.AverageRiskPct);
        }
        Assert.Equal(analysis.Report.ByCareer.Select(c => c.AtRiskPct).OrderByDescending(x => x), analysis.Report.ByCareer.Select(c => c.AtRiskPct));
    }

    [Fact]
    public void AnalyseDropout_WritesRulesInPlainSpanish()
    {
        var report = IntelligenceEngine.AnalyseDropout(Cohort(), 0, TrainedAt).Report;

        Assert.NotEmpty(report.Rules);
        Assert.All(report.Rules, r => Assert.DoesNotContain("≤", r.Condition));
        Assert.Equal(1, report.Importance.Sum(w => w.Importance), precision: 9);
    }

    [Fact]
    public void AnalyseDropout_IsReproducible()
    {
        var cohort = Cohort();

        var first = IntelligenceEngine.AnalyseDropout(cohort, 0, TrainedAt).Report;
        var second = IntelligenceEngine.AnalyseDropout(cohort, 0, TrainedAt).Report;

        Assert.Equal(first.Quality, second.Quality);
        Assert.Equal(first.Rules, second.Rules);
    }

    [Fact]
    public void Report_SurvivesTheTripThroughTheApi()
    {
        var report = IntelligenceEngine.AnalyseDropout(Cohort(), 0, TrainedAt).Report;
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // The API writes camelCase; the Web reads it back with the same defaults.
        var copy = JsonSerializer.Deserialize<DropoutModelReport>(JsonSerializer.Serialize(report, web), web)!;

        Assert.Equal(report.Quality, copy.Quality);
        Assert.Equal(report.Rules, copy.Rules);
        Assert.Equal(report.ByCareer, copy.ByCareer);
        Assert.Equal(report.Tree!.Feature, copy.Tree!.Feature);
        Assert.Equal(report.Tree.Left!.SampleCount, copy.Tree.Left!.SampleCount);
        Assert.Equal(report.TopStudents.Select(s => s.Reasons.Count), copy.TopStudents.Select(s => s.Reasons.Count));
    }

    // ── Forecast ────────────────────────────────────────────────────────────

    [Fact]
    public void Forecast_GraphsOnePeriodAndProjectsFromTwo()
    {
        var onePeriod = IntelligenceEngine.Forecast(History(500), 2);
        Assert.False(onePeriod.HasModel);
        Assert.Equal(new[] { 500d }, onePeriod.Actual);

        var twoPeriods = IntelligenceEngine.Forecast(History(500, 520), 2);
        Assert.True(twoPeriods.HasModel);
        Assert.Equal(540, twoPeriods.NextValue);
        Assert.Same(EnrollmentForecast.None, IntelligenceEngine.Forecast(AcademicHistory.Empty, 2));
    }

    [Fact]
    public void Forecast_ProjectsThePerfectLineAndLabelsTheFuture()
    {
        var history = History([(500, 80, 0), (510, 80, 0), (520, 80, 0)], "2021 Ene-Abr");

        var forecast = IntelligenceEngine.Forecast(history, 3);

        Assert.True(forecast.HasModel);
        Assert.Equal(3, forecast.HistoricalCount);
        Assert.Equal(new[] { "2020 Ene-Abr", "2020 May-Ago", "2020 Sep-Dic", "2021 Ene-Abr", "2021 May-Ago", "2021 Sep-Dic" }, forecast.Labels);
        Assert.Equal(new double[] { 500, 510, 520, 530, 540, 550 }, forecast.Actual);
        Assert.Equal(10, forecast.Slope);
        Assert.Equal(1, forecast.R2);
        Assert.Equal("2021 Ene-Abr", forecast.NextLabel);
        Assert.Equal(530, forecast.NextValue);
        Assert.Equal(0, forecast.NextMargin);
    }

    [Fact]
    public void Forecast_BandWidensAfterTheLastObservation()
    {
        var forecast = IntelligenceEngine.Forecast(History(500, 530, 505, 540, 520), 2);

        var width = forecast.Upper.Zip(forecast.Lower, (u, l) => u - l).ToList();
        Assert.All(Enumerable.Range(0, forecast.Labels.Count),
            i => Assert.True(forecast.Lower[i] <= forecast.Trend[i] && forecast.Trend[i] <= forecast.Upper[i]));
        Assert.True(width[5] > width[4]);
        Assert.True(width[6] > width[5]);
        Assert.True(Math.Abs(width[0] - width[4]) <= 0.2, "the band is constant over observed periods");
    }

    [Fact]
    public void Forecast_NeverProjectsANegativeEnrolment()
    {
        var forecast = IntelligenceEngine.Forecast(History(90, 60, 30), 3);

        Assert.All(forecast.Actual, v => Assert.True(v >= 0));
        Assert.All(forecast.Lower, v => Assert.True(v >= 0));
    }

    // ── Anomalies ───────────────────────────────────────────────────────────

    [Fact]
    public void Anomalies_NeedHistory_AndAFlatSeriesHasNone()
    {
        Assert.Empty(IntelligenceEngine.DetectAnomalies(History(500, 400)));
        Assert.Empty(IntelligenceEngine.DetectAnomalies(History(500, 500, 500, 500, 500)));
    }

    [Fact]
    public void Anomalies_FindASuddenDrop()
    {
        // One outlier among n changes reaches at most √(n−1) deviations, so the series needs length.
        var alerts = IntelligenceEngine.DetectAnomalies(History(500, 510, 520, 530, 460, 470, 480, 490, 500, 510, 520, 530));

        var drop = Assert.Single(alerts, a => a.Title.StartsWith("Caída"));
        Assert.Equal("2021 May-Ago", drop.Period);
        Assert.Equal("error", drop.Severity);
        Assert.True(drop.Magnitude >= 2.5);
    }

    [Fact]
    public void Anomalies_FlagALatestPassRateBelowItsHistory()
    {
        var alerts = IntelligenceEngine.DetectAnomalies(History([(500, 85, 5), (505, 84, 5), (510, 86, 5), (515, 70, 5)]));

        var alert = Assert.Single(alerts, a => a.Title.StartsWith("Aprobación"));
        Assert.Equal("warning", alert.Severity);
        Assert.Equal("2021 Ene-Abr", alert.Period);
        Assert.Equal(15, alert.Magnitude, precision: 1);
    }

    [Fact]
    public void Anomalies_FlagALatestWithdrawalRateAboveItsHistory()
    {
        var alerts = IntelligenceEngine.DetectAnomalies(History([(500, 80, 10), (500, 80, 11), (500, 80, 10), (500, 80, 40)]));

        Assert.Contains(alerts, a => a.Title.StartsWith("Bajas") && a.Period == "2021 Ene-Abr");
    }

    [Fact]
    public void Anomalies_AreRankedAndCapped()
    {
        var alerts = IntelligenceEngine.DetectAnomalies(History(
            [(500, 85, 5), (900, 85, 5), (500, 85, 5), (900, 85, 5), (100, 85, 5), (900, 85, 5), (500, 60, 90)]));

        Assert.True(alerts.Count <= 6);
        Assert.Equal(alerts.Select(a => a.Magnitude).OrderByDescending(m => m), alerts.Select(a => a.Magnitude));
    }
}
