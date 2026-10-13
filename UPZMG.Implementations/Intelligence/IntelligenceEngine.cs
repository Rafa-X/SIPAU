using System.Globalization;

namespace UPZMG.Implementations.Intelligence;

/// <summary>
/// Composes the statistics and the tree into what the institution sees. Pure: it
/// receives already-loaded history, so every rule here is testable without a database.
/// With too little data it says so instead of inventing numbers.
/// </summary>
public static class IntelligenceEngine
{
    public const int MinimumTrainingSamples = 20;
    public const int MaxDepth = 4;
    public const int MinSamplesLeaf = 10;
    public const int Folds = 5;

    // Missing a student who leaves costs far more than one extra tutoring interview,
    // so the cut sits below 0.5 to favour recall.
    public const double RiskThreshold = 0.35;
    public const double HighRiskPct = 50;

    private const int TopStudents = 10;
    private const int MaxAlerts = 6;

    // Periods held back before the retrospective check can say anything. Fewer than this and
    // the fit it is measuring has barely any history to fit.
    public const int BacktestMinimumTrain = 5;

    private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");

    public static string LevelFor(double riskPct) =>
        riskPct >= HighRiskPct ? "Alto" : riskPct >= RiskThreshold * 100 ? "Medio" : "Bajo";

    // ── Enrolment projection ────────────────────────────────────────────────

    public static EnrollmentForecast Forecast(AcademicHistory history, int periodsAhead)
    {
        if (history.Periods.Count == 0) return EnrollmentForecast.None;

        var observed = history.Periods.Select(p => (double)p.Enrolled).ToList();

        // One observed period is useful historical data, but not enough to infer
        // a trend. Return it so the chart can still be rendered.
        if (!history.CanForecast)
        {
            return new EnrollmentForecast(
                history.Periods.Select(p => p.Label).ToList(),
                observed,
                [], [], [],
                observed.Count,
                0, observed[0], null, null,
                HasModel: false);
        }

        // x measured in periods, not in observations: see Positions below.
        var positions = Positions(history);
        var fit = Statistics.Fit(positions, observed);
        if (fit is null) return EnrollmentForecast.None;

        int n = observed.Count;
        int ahead = Math.Max(0, periodsAhead);
        var labels = history.Periods.Select(p => p.Label).Concat(FutureLabels(history, ahead)).ToList();

        var actual = new double[n + ahead];
        var trend = new double[n + ahead];

        // A measured error of zero is a real (zero-width) band; an unmeasurable one is not.
        bool hasBand = fit.StdError is not null;
        var upper = hasBand ? new double[n + ahead] : [];
        var lower = hasBand ? new double[n + ahead] : [];

        for (int i = 0; i < n + ahead; i++)
        {
            var x = i < n ? positions[i] : positions[n - 1] + (i - n + 1);

            trend[i] = Math.Round(fit.PredictAt(x), 1);
            actual[i] = i < n ? observed[i] : Math.Max(0, Math.Round(fit.PredictAt(x)));

            if (!hasBand) continue;

            // ±2 standard errors, widening with each step past the data. Both edges are held at
            // zero, like the projection: a steep enough decline sent the trend below zero, and
            // clamping only the lower edge left an upper bound underneath it.
            double spread = 2 * fit.StdError!.Value * (i < n ? 1 : 1 + 0.35 * (i - n + 1));
            upper[i] = Math.Round(Math.Max(0, trend[i] + spread), 1);
            lower[i] = Math.Round(Math.Max(0, trend[i] - spread), 1);
        }

        return new EnrollmentForecast(
            labels, actual, trend, upper, lower, n,
            Math.Round(fit.Slope, 2), Math.Round(fit.Intercept, 1),
            fit.R2 is { } r2 ? Math.Round(r2, 4) : null,
            fit.StdError is { } error ? Math.Round(error, 2) : null,
            HasModel: true,
            Accuracy: Backtest(history));
    }

    /// <summary>
    /// Where each observed period sits, counted in terms from the first one. Taken from the
    /// label so a term the university did not run stays a gap: numbering the observations
    /// 0, 1, 2… closes the gap up and turns a missing period into a regular step. Labels that
    /// do not follow the "2025 Sep-Dic" shape fall back to the plain sequence.
    /// </summary>
    private static double[] Positions(AcademicHistory history)
    {
        var ordinals = history.Periods
            .Select(p => AcademicHistoryBuilder.TermOrdinal(p.Label))
            .ToList();

        if (ordinals.Any(o => o is null))
            return [.. Enumerable.Range(0, ordinals.Count).Select(i => (double)i)];

        var first = ordinals[0]!.Value;
        return [.. ordinals.Select(o => (double)(o!.Value - first))];
    }

    /// <summary>
    /// What the projection would have got wrong on periods it had not seen. The cut moves
    /// forward one period at a time; at each step the line is fitted only on what came before
    /// and asked for the next period, and two references anybody can compute by hand get the
    /// same treatment. R² measures how well the line describes the history it was fitted to,
    /// which is a different question from how well it predicts the term after.
    /// </summary>
    public static ForecastAccuracy? Backtest(AcademicHistory history)
    {
        var periods = history.Periods;
        if (periods.Count <= BacktestMinimumTrain) return null;

        var observed = periods.Select(p => (double)p.Enrolled).ToArray();
        var positions = Positions(history);

        double trend = 0, lastValue = 0, drift = 0;
        int evaluations = 0;

        for (int cut = BacktestMinimumTrain; cut < observed.Length; cut++)
        {
            var trainY = observed[..cut];
            var trainX = positions[..cut];
            var fit = Statistics.Fit(trainX, trainY);
            if (fit is null) continue;

            var actual = observed[cut];
            var span = trainX[^1] - trainX[0];
            var growthPerPeriod = span <= 0 ? 0 : (trainY[^1] - trainY[0]) / span;
            var step = positions[cut] - trainX[^1];

            trend += Math.Abs(Math.Max(0, fit.PredictAt(positions[cut])) - actual);
            lastValue += Math.Abs(trainY[^1] - actual);
            drift += Math.Abs(Math.Max(0, trainY[^1] + growthPerPeriod * step) - actual);
            evaluations++;
        }

        if (evaluations == 0) return null;

        return new ForecastAccuracy(
            evaluations,
            Math.Round(trend / evaluations, 1),
            Math.Round(lastValue / evaluations, 1),
            Math.Round(drift / evaluations, 1));
    }

    // Known upcoming periods first, then the SEP sequence, then a generic label.
    private static IEnumerable<string> FutureLabels(AcademicHistory history, int count)
    {
        var last = history.Periods[^1].Label;
        for (int i = 0; i < count; i++)
        {
            var next = i < history.UpcomingLabels.Count
                ? history.UpcomingLabels[i]
                : AcademicHistoryBuilder.NextLabel(last) ?? $"+{i + 1}";
            last = next;
            yield return next;
        }
    }

    // ── Dropout model ───────────────────────────────────────────────────────

    /// <summary>
    /// Trains on trajectories that already ended and scores the ones still open.
    /// Active students are never training data: their outcome is unknown, and
    /// calling them "did not leave" would teach the model that everyone enrolled is safe.
    /// </summary>
    public static DropoutAnalysis AnalyseDropout(
        IReadOnlyList<StudentObservation> observations,
        UnscoredReasons unscored,
        string runId,
        DateTime trainedAtUtc)
    {
        var finished = observations.Where(o => o.Outcome is not null).ToList();
        var active = observations.Where(o => o.Outcome is null).ToList();
        int withdrawals = finished.Count(o => o.Outcome == true);
        int graduates = finished.Count - withdrawals;

        var refusal = finished.Count < MinimumTrainingSamples
            ? $"Se necesitan al menos {MinimumTrainingSamples} trayectorias terminadas con calificaciones; hay {finished.Count}."
            : withdrawals < Folds || graduates < Folds
                ? $"Se necesitan al menos {Folds} bajas y {Folds} egresos para entrenar y validar; hay {withdrawals} y {graduates}."
                : null;

        if (refusal is not null)
        {
            // Unscored because there is no model, not because they lack history: the students
            // that do have grades are added to the ones that do not.
            var report = DropoutModelReport.Unavailable(
                refusal, runId, trainedAtUtc, withdrawals, graduates,
                unscored with { NoGrades = unscored.NoGrades + active.Count });

            return new DropoutAnalysis(report, []);
        }

        var samples = finished.Select(o => new Sample(o.Features, o.Outcome!.Value)).ToList();

        // Quality comes from cross-validation; the published tree then learns from every trajectory.
        var quality = DecisionTree.CrossValidate(samples, Folds, RiskThreshold, MaxDepth, MinSamplesLeaf);
        var model = DecisionTree.Train(samples, MaxDepth, MinSamplesLeaf);

        var scored = active
            .Select(o => Score(model, o))
            .OrderByDescending(s => s.RiskPct)
            .ThenBy(s => s.FullName, StringComparer.CurrentCulture)
            .ToList();

        var byCareer = scored
            .GroupBy(s => s.Career)
            .Select(g =>
            {
                var flagged = g.Count(s => s.Level != "Bajo");
                return new CareerRisk(
                    g.Key,
                    g.Count(),
                    flagged,
                    g.Count(s => s.Level == "Alto"),
                    Math.Round((double)flagged / g.Count() * 100, 1),
                    Math.Round(g.Average(s => s.RiskPct), 1));
            })
            .OrderByDescending(c => c.AtRiskPct)
            .ThenBy(c => c.Career, StringComparer.CurrentCulture)
            .ToList();

        var atRisk = scored.Count(s => s.Level != "Bajo");
        var status = $"Entrenado con {finished.Count} trayectorias terminadas ({withdrawals} bajas y {graduates} egresos).";

        var full = new DropoutModelReport(
            HasModel: true,
            RunId: runId,
            Status: status,
            TrainedAtUtc: trainedAtUtc,
            FinishedTrajectories: finished.Count,
            Withdrawals: withdrawals,
            Graduates: graduates,
            Threshold: RiskThreshold,
            Quality: quality,
            Tree: model.Root,
            Rules: model.Rules(DropoutFeatures.Describe),
            Importance: model.FeatureImportance(),
            ByCareer: byCareer,
            TopStudents: [.. scored.Where(s => s.Level != "Bajo").Take(TopStudents)],
            ScoredStudents: scored.Count,
            AtRiskStudents: atRisk,
            HighRiskStudents: scored.Count(s => s.Level == "Alto"),
            Unscored: unscored);

        return new DropoutAnalysis(full, scored);
    }

    private static StudentRisk Score(DecisionTree model, StudentObservation o)
    {
        var features = o.Features;
        var risk = Math.Round(model.Predict(features) * 100, 1);
        var reasons = model.Explain(features, DropoutFeatures.Describe);

        return new StudentRisk(
            o.StudentId, o.EnrollmentNumber, o.FullName, o.Career,
            risk, LevelFor(risk),
            reasons.Count == 0 ? ["El modelo no encontró condiciones que lo distingan"] : reasons,
            Math.Round(o.AverageGrade, 2), Math.Round(o.FailureRate, 1), Math.Round(o.RetakesPerPeriod, 2),
            o.EntryAge, o.Works, o.PeriodsObserved);
    }

    // ── Anomaly detection ───────────────────────────────────────────────────

    /// <summary>
    /// Four independent tests over the observed series:
    /// an unusual period-over-period change (z-score), a period off the fitted trend,
    /// the latest pass rate below its history, and the latest withdrawal rate above it.
    /// </summary>
    public static IReadOnlyList<AnomalyAlert> DetectAnomalies(AcademicHistory history)
    {
        var periods = history.Periods;
        if (periods.Count < 3) return [];

        var found = new List<AnomalyAlert>();
        var enrolment = periods.Select(p => (double)p.Enrolled).ToList();

        // 1. Change unlike any other. The first period has no change, so it stays out of the stats.
        var changes = Statistics.PercentChange(enrolment);
        var z = Statistics.ZScores(changes[1..]);
        for (int i = 1; i < periods.Count; i++)
        {
            var score = z[i - 1];
            if (Math.Abs(score) < 1.8) continue;

            var direction = changes[i] < 0 ? "Caída" : "Alza";
            var severity = Math.Abs(score) >= 2.5 ? "error" : "warning";
            found.Add(new AnomalyAlert(
                $"{direction} atípica de matrícula en {periods[i].Label}",
                $"{Format(changes[i], "+0.0;-0.0")} % respecto al periodo anterior, "
                + $"{Format(Math.Abs(score), "0.0")} desviaciones estándar fuera del cambio habitual.",
                severity,
                Math.Round(Math.Abs(score), 1),
                periods[i].Label,
                AlertKey("enrolment-change", periods[i].Label, severity)));
        }

        // 2. Off the institution's own trend.
        var fit = Statistics.Fit(enrolment);
        if (fit is { StdError: > 0 })
        {
            for (int i = 0; i < enrolment.Count; i++)
            {
                var residual = enrolment[i] - fit.PredictAt(i);
                var sigmas = residual / fit.StdError.Value;
                if (Math.Abs(sigmas) < 2) continue;

                found.Add(new AnomalyAlert(
                    $"Matrícula fuera de tendencia en {periods[i].Label}",
                    $"Se observaron {Format(enrolment[i], "N0")} alumnos y la recta esperaba "
                    + $"{Format(fit.PredictAt(i), "N0")} ({Format(residual, "+0;-0")}).",
                    "info",
                    Math.Round(Math.Abs(sigmas), 1),
                    periods[i].Label,
                    AlertKey("enrolment-trend", periods[i].Label, "info")));
            }
        }

        var last = periods[^1];
        var previous = periods.Take(periods.Count - 1).ToList();

        // 3. Latest pass rate against the earlier ones. A period with no grades has no rate,
        // so it is neither compared nor used as a reference.
        var passRates = previous.Where(p => p.HasGrades).Select(p => p.PassRate!.Value).ToList();
        if (passRates.Count >= 3 && last.PassRate is { } latestPassRate)
        {
            var mean = passRates.Average();
            if (latestPassRate < mean - 5)
            {
                found.Add(new AnomalyAlert(
                    $"Aprobación por debajo de su historial en {last.Label}",
                    $"{Format(latestPassRate, "0.0")} % de aprobación frente a un promedio previo de {Format(mean, "0.0")} %.",
                    "warning",
                    Math.Round(mean - latestPassRate, 1),
                    last.Label,
                    AlertKey("pass-rate", last.Label, "warning")));
            }
        }

        // 4. Latest withdrawal rate against the earlier ones.
        var withdrawalRates = previous.Select(p => p.WithdrawalRate).ToList();
        if (withdrawalRates.Count >= 3)
        {
            var mean = withdrawalRates.Average();
            var sd = Statistics.StandardDeviation(withdrawalRates);
            if (last.WithdrawalRate > mean + Math.Max(2 * sd, 2))
            {
                found.Add(new AnomalyAlert(
                    $"Bajas por encima de lo habitual en {last.Label}",
                    $"{Format(last.WithdrawalRate, "0.0")} % de los inscritos se dio de baja; el promedio previo es {Format(mean, "0.0")} %.",
                    "warning",
                    Math.Round(last.WithdrawalRate - mean, 1),
                    last.Label,
                    AlertKey("withdrawals", last.Label, "warning")));
            }
        }

        // Ordered by how much it deserves attention, not by the bare number. The magnitudes
        // come from different families — standard deviations, percentage points — and comparing
        // them directly let a mild deviation of one family outrank a severe one of another.
        return
        [
            .. found
                .OrderByDescending(a => SeverityRank(a.Severity))
                .ThenByDescending(a => a.Period == last.Label)
                .ThenByDescending(a => a.Magnitude)
                .Take(MaxAlerts)
        ];
    }

    // Test, period and severity: enough to recognise the same deviation on the next read.
    private static string AlertKey(string test, string period, string severity) => $"{test}|{period}|{severity}";

    private static int SeverityRank(string severity) => severity switch
    {
        "error" => 2,
        "warning" => 1,
        _ => 0,
    };

    private static string Format(double value, string format) => value.ToString(format, Mx);
}
