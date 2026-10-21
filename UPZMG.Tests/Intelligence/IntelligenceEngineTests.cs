using System.Text.Json;
using UPZMG.Implementations.Intelligence;
using Xunit;

namespace UPZMG.Tests.Intelligence;

// The business rules on top of the maths: minimums, thresholds, scoring and alerts.
public class IntelligenceEngineTests
{
    private static readonly DateTime TrainedAt = new(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

    // Identifies the cut of the data a training read; any constant will do in a test.
    private const string Run = "test0001";

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

    // Lower averages leave more often, but not always: every cell of the matrix gets something.
    private static List<StudentObservation> NoisyCohort()
    {
        var rng = new Random(3);
        var list = new List<StudentObservation>();

        for (int n = 0; n < 400; n++)
        {
            var average = rng.Next(50, 100) / 10.0;
            var failure = Math.Clamp((8.5 - average) * 15 + rng.Next(-10, 10), 0, 100);
            bool? outcome = n < 300 ? rng.NextDouble() < (average < 7 ? 0.5 : 0.08) : null;
            list.Add(Observation(average, failure, rng.Next(0, 3) == 0, outcome, n));
        }

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
        Assert.Equal(expected, IntelligenceEngine.LevelFor(risk, threshold: 0.35));
    }

    [Theory]
    [InlineData(55, "Bajo")]
    [InlineData(60, "Alto")]
    public void LevelFor_NeverCallsHighAScoreBelowTheThreshold(double risk, string expected)
    {
        Assert.Equal(expected, IntelligenceEngine.LevelFor(risk, threshold: 0.6));
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
    [InlineData("Oportunidades extra por periodo", null, 2.5, "Oportunidades extra por periodo de 2.5 o menos")]
    public void Features_ReadAsPlainSpanish(string feature, double? above, double? atMost, string expected)
    {
        Assert.Equal(expected, DropoutFeatures.Describe(feature, above, atMost));
    }

    // ── Dropout model ───────────────────────────────────────────────────────

    [Fact]
    public void AnalyseDropout_RefusesWithTooFewFinishedTrajectories()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(Cohort(finishedPerClass: 9), new UnscoredReasons(1, 2), Run, TrainedAt);

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

        var report = IntelligenceEngine.AnalyseDropout(observations, UnscoredReasons.None, Run, TrainedAt).Report;

        Assert.False(report.HasModel);
        Assert.Equal(4, report.Withdrawals);
        Assert.Contains("bajas y", report.Status);
    }

    [Fact]
    public void AnalyseDropout_LearnsTheSignalAndScoresOnlyActiveStudents()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(Cohort(), new UnscoredReasons(0, 2), Run, TrainedAt);
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
    public void ChooseThreshold_TakesTheCheapestCutAndTheLowestOnATie()
    {
        // Two withdrawals scored 0.6 and 0.3; three graduates scored 0.3, 0.1 and 0.1.
        OutOfFoldScore[] scores =
        [
            new(0, 0, 0.6, true), new(0, 1, 0.3, true), new(0, 2, 0.3, false), new(0, 3, 0.1, false), new(0, 4, 0.1, false),
        ];

        // From 0.15 to 0.3 one extra alert and nothing missed: cost 1, the cheapest, and 0.15 the lowest.
        // From 0.35 on one withdrawal is missed (3); at 0.1 or below three extra alerts (3).
        Assert.Equal(0.15, IntelligenceEngine.ChooseThreshold(scores));

        // Were a missed withdrawal worth half an interview, missing it would be the cheaper error.
        Assert.Equal(0.35, IntelligenceEngine.ChooseThreshold(scores, falseNegativeCost: 0.5));
    }

    [Fact]
    public void AnalyseDropout_ReportsHowItWasMeasured()
    {
        var report = IntelligenceEngine.AnalyseDropout(NoisyCohort(), UnscoredReasons.None, Run, TrainedAt).Report;
        var evaluation = report.Evaluation!;
        var quality = report.Quality;

        Assert.Equal(IntelligenceEngine.Seed, evaluation.Seed);

        // The folds add up to the pooled matrix, and each was graded by a tree that learnt from the rest.
        Assert.Equal(IntelligenceEngine.Folds, evaluation.Folds.Count);
        Assert.Equal(quality.TruePositives, evaluation.Folds.Sum(f => f.Quality.TruePositives));
        Assert.Equal(quality.FalsePositives, evaluation.Folds.Sum(f => f.Quality.FalsePositives));
        Assert.Equal(quality.TrueNegatives, evaluation.Folds.Sum(f => f.Quality.TrueNegatives));
        Assert.Equal(quality.FalseNegatives, evaluation.Folds.Sum(f => f.Quality.FalseNegatives));
        Assert.All(evaluation.Folds, f => Assert.Equal(report.FinishedTrajectories, f.TrainingSize + f.Quality.Evaluated));

        // Every candidate is graded, and the published threshold is the lowest of the cheapest.
        Assert.Equal(IntelligenceEngine.CandidateThresholds, evaluation.Thresholds.Select(t => t.Threshold));
        var cheapest = evaluation.Thresholds.Min(t => t.Cost);
        Assert.Equal(evaluation.Thresholds.Where(t => t.Cost == cheapest).Min(t => t.Threshold), report.Threshold);
        Assert.Equal(quality, evaluation.Thresholds.Single(t => t.Threshold == report.Threshold).Quality);
        Assert.Contains(evaluation.Sensitivity, c => c.FalseNegativeCost == IntelligenceEngine.FalseNegativeCost && c.Threshold == report.Threshold);

        // The nested estimate grades every trajectory once, with the threshold its fold chose blind.
        Assert.Equal(report.FinishedTrajectories, evaluation.Nested.Evaluated);
        Assert.Equal(IntelligenceEngine.Folds, evaluation.NestedThresholds.Count);
        Assert.All(evaluation.NestedThresholds, t => Assert.Contains(t, IntelligenceEngine.CandidateThresholds));

        // The four cells of the matrix, in order, cover every trajectory.
        Assert.Equal(["Bajas detectadas", "Bajas no detectadas", "Alertas de más", "Bien descartados"], evaluation.Errors.Select(e => e.Group));
        Assert.Equal(
            [quality.TruePositives, quality.FalseNegatives, quality.FalsePositives, quality.TrueNegatives],
            evaluation.Errors.Select(e => e.Count));
    }

    [Fact]
    public void AnalyseDropout_MarksAsUncertainTheScoresWhoseIntervalCrossesTheThreshold()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(NoisyCohort(), UnscoredReasons.None, Run, TrainedAt);
        var threshold = analysis.Report.Threshold;

        Assert.All(analysis.Students, s =>
        {
            var (lower, upper) = Statistics.Wilson(s.RuleWithdrawals, s.RuleSupport);
            Assert.Equal(lower < threshold && threshold <= upper, s.Uncertain);
            Assert.Equal(Math.Round((double)s.RuleWithdrawals / s.RuleSupport * 100, 1), s.RiskPct);
            Assert.InRange(s.RiskPct, s.LowerPct, s.UpperPct);
        });

        Assert.Contains(analysis.Students, s => s.Uncertain);
        Assert.Equal(analysis.Students.Count(s => s.Uncertain), analysis.Report.UncertainStudents);
    }

    [Fact]
    public void AnalyseDropout_SummarisesEachCareer()
    {
        var analysis = IntelligenceEngine.AnalyseDropout(Cohort(), UnscoredReasons.None, Run, TrainedAt);

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
        var report = IntelligenceEngine.AnalyseDropout(Cohort(), UnscoredReasons.None, Run, TrainedAt).Report;

        Assert.NotEmpty(report.Rules);
        Assert.All(report.Rules, r => Assert.DoesNotContain("≤", r.Condition));
        Assert.Equal(1, report.Importance.Sum(w => w.Importance), precision: 9);
    }

    [Fact]
    public void AnalyseDropout_IsReproducible()
    {
        var cohort = Cohort();

        var first = IntelligenceEngine.AnalyseDropout(cohort, UnscoredReasons.None, Run, TrainedAt).Report;
        var second = IntelligenceEngine.AnalyseDropout(cohort, UnscoredReasons.None, Run, TrainedAt).Report;

        Assert.Equal(first.Quality, second.Quality);
        Assert.Equal(first.Rules, second.Rules);
    }

    [Fact]
    public void Report_SurvivesTheTripThroughTheApi()
    {
        var report = IntelligenceEngine.AnalyseDropout(Cohort(), UnscoredReasons.None, Run, TrainedAt).Report;
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // The API writes camelCase; the Web reads it back with the same defaults.
        var copy = JsonSerializer.Deserialize<DropoutModelReport>(JsonSerializer.Serialize(report, web), web)!;

        Assert.Equal(report.Quality, copy.Quality);
        Assert.Equal(report.Rules, copy.Rules);
        Assert.Equal(report.ByCareer, copy.ByCareer);
        Assert.Equal(report.UncertainStudents, copy.UncertainStudents);
        Assert.Equal(report.Evaluation!.Folds, copy.Evaluation!.Folds);
        Assert.Equal(report.Evaluation.Thresholds, copy.Evaluation.Thresholds);
        Assert.Equal(report.Evaluation.Sensitivity, copy.Evaluation.Sensitivity);
        Assert.Equal(report.Evaluation.Nested, copy.Evaluation.Nested);
        Assert.Equal(report.Evaluation.NestedThresholds, copy.Evaluation.NestedThresholds);
        Assert.Equal(report.Evaluation.Errors, copy.Evaluation.Errors);
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
    public void Forecast_BandIsThePredictionIntervalAndWidensAwayFromTheData()
    {
        var forecast = IntelligenceEngine.Forecast(History(500, 530, 505, 540, 520), 2);

        var width = forecast.Upper.Zip(forecast.Lower, (u, l) => u - l).ToList();
        Assert.All(Enumerable.Range(0, forecast.Labels.Count),
            i => Assert.True(forecast.Lower[i] <= forecast.Trend[i] && forecast.Trend[i] <= forecast.Upper[i]));

        // Narrowest at the centre of the history, wider at its ends and wider still past them.
        Assert.True(width[2] < width[0] && width[2] < width[4]);
        Assert.True(width[5] > width[4]);
        Assert.True(width[6] > width[5]);

        // Each edge is the projection plus t(0.975, n − 2) · s · √(1 + 1/n + (x − x̄)² / Sxx).
        var fit = Statistics.Fit([500, 530, 505, 540, 520])!;
        Assert.Equal(Math.Round(Math.Round(fit.PredictAt(5), 1) + fit.PredictionMargin(5)!.Value, 1), forecast.Upper[5]);
    }

    [Fact]
    public void Forecast_ChecksItsIntervalOnThePeriodsItDidNotSee()
    {
        // Steady growth with some noise: every held-out period lands inside the interval drawn before it.
        var steady = IntelligenceEngine.Forecast(History(500, 512, 519, 533, 538, 552, 559, 571), 1).Accuracy!;
        Assert.Equal(3, steady.Evaluations);
        Assert.Equal(3, steady.InsideInterval);

        // A collapse no line could foresee falls outside it.
        var shock = IntelligenceEngine.Forecast(History(500, 510, 521, 529, 541, 300), 1).Accuracy!;
        Assert.Equal(1, shock.Evaluations);
        Assert.Equal(0, shock.InsideInterval);
    }

    [Fact]
    public void Forecast_NeverProjectsANegativeEnrolment()
    {
        var forecast = IntelligenceEngine.Forecast(History(90, 60, 30), 3);

        Assert.All(forecast.Actual, v => Assert.True(v >= 0));
        Assert.All(forecast.Lower, v => Assert.True(v >= 0));

        // Both edges are held at zero, so the band can never come out upside down.
        Assert.All(Enumerable.Range(0, forecast.Upper.Count),
            i => Assert.True(forecast.Upper[i] >= forecast.Lower[i]));
    }

    [Fact]
    public void Forecast_MeasuresItselfAgainstTheReferencesOnPeriodsItDidNotSee()
    {
        // Six periods: one of them can be held back and predicted without having been seen.
        var accuracy = IntelligenceEngine.Forecast(History(500, 510, 520, 530, 540, 551), 2).Accuracy;

        Assert.NotNull(accuracy);
        Assert.Equal(1, accuracy.Evaluations);

        // A straight line is exactly what the trend fits, so here it wins.
        Assert.True(accuracy.TrendError < accuracy.LastValueError);
        Assert.Equal("la tendencia lineal", accuracy.Best);
        Assert.False(accuracy.TrendIsBeaten);
    }

    [Fact]
    public void Forecast_HasNoRetrospectiveErrorUntilThereIsHistoryToHoldBack()
    {
        Assert.Null(IntelligenceEngine.Forecast(History(500, 510, 520), 2).Accuracy);
    }

    [Fact]
    public void Forecast_CountsAMissingPeriodAsAGapAndNotAsAStep()
    {
        // 2020 Ene-Abr, 2020 May-Ago and then — nothing in Sep-Dic — 2021 Ene-Abr.
        var periods = new[] { ("2020 Ene-Abr", 500), ("2020 May-Ago", 510), ("2021 Ene-Abr", 530) }
            .Select((p, i) => new PeriodStats(Guid.NewGuid(), p.Item1, new DateOnly(2020 + i, 1, 1),
                p.Item2, 0, 0, 0, 800, 200))
            .ToList();

        var forecast = IntelligenceEngine.Forecast(new AcademicHistory(periods, []), 1);

        // 30 students over three periods, not over two observations: +10 per period.
        Assert.Equal(10, forecast.Slope);
        Assert.Equal(540, forecast.NextValue);
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
    public void Anomalies_CarryAKeyThatSurvivesACorrectionOfTheFigures()
    {
        var first = IntelligenceEngine.DetectAnomalies(History([(500, 85, 5), (505, 84, 5), (510, 86, 5), (515, 70, 5)]));
        var corrected = IntelligenceEngine.DetectAnomalies(History([(500, 85, 5), (505, 84, 5), (510, 86, 5), (515, 71, 5)]));

        var before = Assert.Single(first, a => a.Title.StartsWith("Aprobación"));
        var after = Assert.Single(corrected, a => a.Title.StartsWith("Aprobación"));

        // The detail changed with the figure; what the alert is about did not. A mark a person
        // left on it has to hold.
        Assert.NotEqual(before.Detail, after.Detail);
        Assert.Equal(before.Key, after.Key);
        Assert.Contains("2021 Ene-Abr", before.Key);
    }

    [Fact]
    public void Anomalies_HaveDistinctKeys()
    {
        var alerts = IntelligenceEngine.DetectAnomalies(History(
            [(500, 85, 5), (900, 85, 5), (500, 85, 5), (900, 85, 5), (100, 85, 5), (900, 85, 5), (500, 60, 90)]));

        Assert.NotEmpty(alerts);
        Assert.All(alerts, a => Assert.False(string.IsNullOrWhiteSpace(a.Key)));
        Assert.Equal(alerts.Count, alerts.Select(a => a.Key).Distinct().Count());
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
