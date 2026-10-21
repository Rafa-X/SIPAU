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

    // Seed of the stratified shuffle behind the folds: the same data gives the same figures.
    public const int Seed = 20260917;

    // Candidate cuts the threshold study grades, every 0.05 from 0.05 to 0.95.
    public static readonly IReadOnlyList<double> CandidateThresholds = [.. Enumerable.Range(1, 19).Select(i => i / 20.0)];

    // What a missed withdrawal costs, counted in follow-up interviews that were not needed. The
    // decision threshold is the candidate that makes extra alerts + FalseNegativeCost · missed
    // withdrawals smallest over trajectories the tree did not see; this number, not a hand-picked
    // cut, is the policy decision, and the sensitivity study shows what other values would pick.
    public const double FalseNegativeCost = 3;
    public static readonly IReadOnlyList<double> SensitivityCosts = [1.5, 2, 3, 4, 5];

    public const double HighRiskPct = 50;

    /// <summary>The threshold rule, in the words the screens use.</summary>
    public static string ThresholdRule =>
        $"De los cortes entre 5 % y 95 %, de 5 en 5, el que hace mínimo el costo de equivocarse sobre las "
        + $"trayectorias que el árbol no vio: cada alerta de más cuesta 1 (una entrevista de tutoría que no hacía "
        + $"falta) y cada baja no detectada cuesta {FalseNegativeCost.ToString("0.#", CultureInfo.InvariantCulture)}. "
        + "Si varios empatan, el más bajo.";

    private const int TopStudents = 10;
    private const int MaxAlerts = 6;

    // Periods held back before the retrospective check can say anything. Fewer than this and
    // the fit it is measuring has barely any history to fit.
    public const int BacktestMinimumTrain = 5;

    private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");

    // "Alto" never sits below the threshold: a student the model does not flag is not high risk.
    public static string LevelFor(double riskPct, double threshold) =>
        riskPct >= Math.Max(HighRiskPct, threshold * 100) ? "Alto" : riskPct >= threshold * 100 ? "Medio" : "Bajo";

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

            // The 95 % prediction interval of the regression: where one period at x would land
            // 95 times in 100 if the line holds. It widens on its own as x leaves the fitted
            // history, because the slope's uncertainty grows with the distance. Both edges are
            // held at zero, like the projection: a steep enough decline sent the trend below
            // zero, and clamping only the lower edge left an upper bound underneath it.
            double margin = fit.PredictionMargin(x) ?? 0;
            upper[i] = Math.Round(Math.Max(0, trend[i] + margin), 1);
            lower[i] = Math.Round(Math.Max(0, trend[i] - margin), 1);
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
    /// which is a different question from how well it predicts the term after. Each step also
    /// checks whether the period fell inside the 95 % interval drawn from the same cut.
    /// </summary>
    public static ForecastAccuracy? Backtest(AcademicHistory history)
    {
        var periods = history.Periods;
        if (periods.Count <= BacktestMinimumTrain) return null;

        var observed = periods.Select(p => (double)p.Enrolled).ToArray();
        var positions = Positions(history);

        double trend = 0, lastValue = 0, drift = 0;
        int evaluations = 0, inside = 0;

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

            var predicted = fit.PredictAt(positions[cut]);
            trend += Math.Abs(Math.Max(0, predicted) - actual);
            lastValue += Math.Abs(trainY[^1] - actual);
            drift += Math.Abs(Math.Max(0, trainY[^1] + growthPerPeriod * step) - actual);
            evaluations++;

            // The interval as the chart would have drawn it at that cut, edges held at zero.
            var margin = fit.PredictionMargin(positions[cut]) ?? 0;
            if (Math.Max(0, predicted - margin) <= actual && actual <= Math.Max(0, predicted + margin)) inside++;
        }

        if (evaluations == 0) return null;

        return new ForecastAccuracy(
            evaluations,
            Math.Round(trend / evaluations, 1),
            Math.Round(lastValue / evaluations, 1),
            Math.Round(drift / evaluations, 1),
            inside);
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
        // The refusal above guarantees a sample of each class per fold, so the run always exists.
        var run = DecisionTree.CrossValidateScores(samples, Folds, MaxDepth, MinSamplesLeaf, Seed)!;
        var threshold = ChooseThreshold(run.Scores);
        var quality = DecisionTree.Confusion(run.Scores, threshold, samples.Count, Folds);
        var evaluation = Evaluate(samples, run, threshold);
        var model = DecisionTree.Train(samples, MaxDepth, MinSamplesLeaf);

        var scored = active
            .Select(o => Score(model, o, threshold))
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
            Threshold: threshold,
            Quality: quality,
            Tree: model.Root,
            Rules: model.Rules(DropoutFeatures.Describe),
            Importance: model.FeatureImportance(),
            ByCareer: byCareer,
            TopStudents: [.. scored.Where(s => s.Level != "Bajo").Take(TopStudents)],
            ScoredStudents: scored.Count,
            AtRiskStudents: atRisk,
            HighRiskStudents: scored.Count(s => s.Level == "Alto"),
            Unscored: unscored,
            UncertainStudents: scored.Count(s => s.Uncertain),
            Evaluation: evaluation);

        return new DropoutAnalysis(full, scored);
    }

    /// <summary>
    /// The evidence behind the quality figures: the matrix of each fold, every candidate
    /// threshold, what the misclassified trajectories look like, and the threshold rule graded
    /// without letting it see the fold it is graded on.
    /// </summary>
    public static ModelEvaluation Evaluate(IReadOnlyList<Sample> samples, CrossValidationRun run, double threshold)
    {
        var folds = Enumerable.Range(0, run.Folds)
            .Select(k => new FoldResult(
                k + 1,
                run.TrainingSizes[k],
                DecisionTree.Confusion(run.Scores.Where(s => s.Fold == k), threshold, run.TrainingSizes[k], run.Folds)))
            .ToList();

        var thresholds = CandidateThresholds
            .Select(t => new ThresholdPoint(t, DecisionTree.Confusion(run.Scores, t, samples.Count, run.Folds)))
            .ToList();

        var sensitivity = SensitivityCosts
            .Select(cost =>
            {
                var chosen = ChooseThreshold(run.Scores, cost);
                return new CostChoice(cost, chosen, DecisionTree.Confusion(run.Scores, chosen, samples.Count, run.Folds));
            })
            .ToList();

        // Nested: the rule picks a threshold from an inner cross-validation of the other folds,
        // and that threshold is graded on the fold the rule never looked at.
        var nestedThresholds = new List<double>(run.Folds);
        var nestedScores = new List<OutOfFoldScore>(run.Scores.Count);
        var nestedFlags = new List<bool>(run.Scores.Count);

        for (int k = 0; k < run.Folds; k++)
        {
            var training = run.Scores.Where(s => s.Fold != k).Select(s => samples[s.Index]).ToList();
            var inner = DecisionTree.CrossValidateScores(training, run.Folds, MaxDepth, MinSamplesLeaf, Seed);
            var chosen = inner is null ? threshold : ChooseThreshold(inner.Scores);
            nestedThresholds.Add(chosen);

            foreach (var score in run.Scores.Where(s => s.Fold == k))
            {
                nestedScores.Add(score);
                nestedFlags.Add(score.Probability >= chosen);
            }
        }

        return new ModelEvaluation(
            Seed,
            folds,
            thresholds,
            sensitivity,
            Matrix(nestedScores.Zip(nestedFlags), samples.Count, run.Folds),
            nestedThresholds,
            Profiles(samples, run.Scores, threshold));
    }

    /// <summary>
    /// The selection rule: the candidate with the smallest cost over the held-out scores, the
    /// lowest one when several tie. Leaf probabilities are discrete, so neighbouring candidates
    /// often flag exactly the same trajectories.
    /// </summary>
    public static double ChooseThreshold(IReadOnlyList<OutOfFoldScore> scores, double falseNegativeCost = FalseNegativeCost) =>
        CandidateThresholds
            .Select(t => (Threshold: t, Cost: DecisionTree.Confusion(scores, t, scores.Count, 0).Cost(falseNegativeCost)))
            .OrderBy(c => c.Cost)
            .ThenBy(c => c.Threshold)
            .First()
            .Threshold;

    private static ModelQuality Matrix(IEnumerable<(OutOfFoldScore Score, bool Flagged)> decisions, int trainingSize, int folds)
    {
        int tp = 0, fp = 0, tn = 0, fn = 0;
        foreach (var (score, flagged) in decisions)
        {
            if (flagged && score.Label) tp++;
            else if (flagged) fp++;
            else if (score.Label) fn++;
            else tn++;
        }

        return new ModelQuality(tp, fp, tn, fn, trainingSize, folds);
    }

    // The four cells of the matrix, each described by the mean of every input.
    private static IReadOnlyList<ErrorProfile> Profiles(
        IReadOnlyList<Sample> samples, IReadOnlyList<OutOfFoldScore> scores, double threshold)
    {
        string CellOf(OutOfFoldScore s) => (s.Probability >= threshold, s.Label) switch
        {
            (true, true) => "Bajas detectadas",
            (false, true) => "Bajas no detectadas",
            (true, false) => "Alertas de más",
            _ => "Bien descartados",
        };

        string[] order = ["Bajas detectadas", "Bajas no detectadas", "Alertas de más", "Bien descartados"];

        return
        [
            .. scores
                .GroupBy(CellOf)
                .OrderBy(g => Array.IndexOf(order, g.Key))
                .Select(g =>
                {
                    var features = g.Select(s => samples[s.Index].Features).ToList();
                    double Mean(string name) => Math.Round(features.Average(f => f[name]), 2);

                    return new ErrorProfile(
                        g.Key,
                        g.Count(),
                        Mean(DropoutFeatures.Average),
                        Mean(DropoutFeatures.FailureRate),
                        Mean(DropoutFeatures.RetakesPerPeriod),
                        Mean(DropoutFeatures.EntryAge),
                        Math.Round(features.Average(f => f[DropoutFeatures.Works]) * 100, 1));
                }),
        ];
    }

    private static StudentRisk Score(DecisionTree model, StudentObservation o, double threshold)
    {
        // One walk down the tree gives the number, the rule and how many trajectories back it.
        var decision = model.Decide(o.Features, DropoutFeatures.Describe);
        var risk = Math.Round(decision.Probability * 100, 1);
        var (lower, upper) = Statistics.Wilson(decision.Positives, decision.Support);

        return new StudentRisk(
            o.StudentId, o.EnrollmentNumber, o.FullName, o.Career,
            risk, LevelFor(risk, threshold),
            decision.Conditions.Count == 0 ? ["El modelo no encontró condiciones que lo distingan"] : decision.Conditions,
            Math.Round(o.AverageGrade, 2), Math.Round(o.FailureRate, 1), Math.Round(o.RetakesPerPeriod, 2),
            o.EntryAge, o.Works, o.PeriodsObserved,
            RuleSupport: decision.Support,
            RuleWithdrawals: decision.Positives,
            LowerPct: Math.Round(lower * 100, 1),
            UpperPct: Math.Round(upper * 100, 1),
            Uncertain: lower < threshold && threshold <= upper);
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
