namespace UPZMG.Implementations.Intelligence;

/// <summary>Least-squares projection of enrolment over the observed periods.</summary>
/// <param name="Actual">Observed values, then the projection for the periods ahead.</param>
/// <param name="Trend">The fitted line ŷ = a + b·x at every period.</param>
/// <param name="Upper">Upper edge of the ±2 standard error band; empty when the error cannot be measured.</param>
/// <param name="Lower">Lower edge of the band, never below zero; empty when the error cannot be measured.</param>
/// <param name="HistoricalCount">How many leading entries are observed rather than projected.</param>
/// <param name="R2">Null with only two observed periods: any line fits them exactly.</param>
/// <param name="StdError">Null with only two observed periods; then the projection carries no band.</param>
/// <param name="HasModel">False when there was too little history to fit anything.</param>
/// <param name="Accuracy">
/// How the projection did against the periods it had not seen. Null until there is enough
/// history to hold some back.
/// </param>
public sealed record EnrollmentForecast(
    IReadOnlyList<string> Labels,
    IReadOnlyList<double> Actual,
    IReadOnlyList<double> Trend,
    IReadOnlyList<double> Upper,
    IReadOnlyList<double> Lower,
    int HistoricalCount,
    double Slope,
    double Intercept,
    double? R2,
    double? StdError,
    bool HasModel,
    ForecastAccuracy? Accuracy = null)
{
    public static readonly EnrollmentForecast None = new([], [], [], [], [], 0, 0, 0, null, null, false);

    // The first projected period, when there is one.
    public string? NextLabel => HasModel && Labels.Count > HistoricalCount ? Labels[HistoricalCount] : null;
    public double? NextValue => HasModel && Actual.Count > HistoricalCount ? Actual[HistoricalCount] : null;
    public double? NextMargin => HasModel && Upper.Count > HistoricalCount
        ? Math.Round(Upper[HistoricalCount] - Trend[HistoricalCount], 1)
        : null;
}

/// <summary>
/// What the projection would have got wrong, measured by moving the cut forward: for every
/// period from the sixth on, the line is fitted only on what was known before it and asked to
/// predict it. Two references anybody can compute by hand are measured the same way, because
/// R² describes the history the line was fitted to and says nothing about the next period.
/// </summary>
/// <param name="Evaluations">How many periods were predicted without having been seen.</param>
/// <param name="TrendError">Mean absolute error of the published projection, in students.</param>
/// <param name="LastValueError">Error of repeating the last observed enrolment.</param>
/// <param name="DriftError">Error of the last value plus the average growth per period.</param>
public sealed record ForecastAccuracy(
    int Evaluations,
    double TrendError,
    double LastValueError,
    double DriftError)
{
    /// <summary>The smallest of the three errors over the same periods.</summary>
    public double BestError => Math.Min(TrendError, Math.Min(LastValueError, DriftError));

    /// <summary>Which candidate achieved it, named as the screens name them.</summary>
    public string Best =>
        TrendError <= LastValueError && TrendError <= DriftError ? "la tendencia lineal"
        : DriftError <= LastValueError ? "el último valor más el crecimiento promedio"
        : "repetir el último valor";

    /// <summary>True while the published projection is not the most accurate candidate.</summary>
    public bool TrendIsBeaten => TrendError > BestError;
}

/// <summary>One active student scored by the tree, with the rule path behind the score.</summary>
public sealed record StudentRisk(
    Guid StudentId,
    string EnrollmentNumber,
    string FullName,
    string Career,
    double RiskPct,
    string Level,
    IReadOnlyList<string> Reasons,
    double AverageGrade,
    double FailureRate,
    double RetakesPerPeriod,
    int EntryAge,
    bool Works,
    int PeriodsObserved);

/// <summary>Scored students of one programme.</summary>
/// <param name="AtRiskPct">Share of its students flagged (medium or high risk).</param>
/// <param name="AverageRiskPct">Mean probability of withdrawal across its students.</param>
public sealed record CareerRisk(
    string Career,
    int Students,
    int AtRisk,
    int HighRisk,
    double AtRiskPct,
    double AverageRiskPct);

/// <summary>What the dropout model is, how well it performs and whom it flags.</summary>
/// <param name="RunId">The cut of the data this training read; shared with the student list.</param>
/// <param name="Status">One line: why there is no model, or what it was trained on.</param>
/// <param name="Threshold">Probability from which a student counts as at risk.</param>
/// <param name="Target">What the model was trained to recognise, in one line.</param>
/// <param name="Unscored">Active students with no score, broken down by why.</param>
public sealed record DropoutModelReport(
    bool HasModel,
    string RunId,
    string Status,
    DateTime TrainedAtUtc,
    int FinishedTrajectories,
    int Withdrawals,
    int Graduates,
    double Threshold,
    ModelQuality Quality,
    TreeNode? Tree,
    IReadOnlyList<TreeRule> Rules,
    IReadOnlyList<FeatureWeight> Importance,
    IReadOnlyList<CareerRisk> ByCareer,
    IReadOnlyList<StudentRisk> TopStudents,
    int ScoredStudents,
    int AtRiskStudents,
    int HighRiskStudents,
    UnscoredReasons Unscored)
{
    /// <summary>
    /// What the percentage means, said once so no screen has to guess. The model separates
    /// trajectories that ended in a definitive baja from those that ended in graduation, using
    /// the last period each student had grades in; it is not "probability of leaving next term".
    /// </summary>
    public string Target =>
        "Baja definitiva frente a egreso, aprendido del último periodo con calificaciones de cada "
        + "trayectoria terminada. No es una probabilidad de abandono en el periodo siguiente.";

    public int UnscoredActiveStudents => Unscored.Total;

    public static DropoutModelReport Unavailable(
        string status, string runId, DateTime trainedAtUtc, int withdrawals, int graduates, UnscoredReasons unscored) =>
        new(false, runId, status, trainedAtUtc, withdrawals + graduates, withdrawals, graduates,
            IntelligenceEngine.RiskThreshold, ModelQuality.None, null, [], [], [], [], 0, 0, 0, unscored);
}

/// <summary>A statistically detected deviation in the institutional series.</summary>
/// <param name="Severity">info, warning or error.</param>
/// <param name="Magnitude">How far out it sits (standard deviations or points), used to rank alerts.</param>
/// <param name="Key">
/// What the alert is about — the test that raised it and the period — so the same deviation is
/// recognised the next time the series are read. The figures in the detail are left out on
/// purpose: a corrected grade must not bring back an alert somebody already dealt with. The
/// severity is part of it, because an alert that got worse deserves to be seen again.
/// </param>
public sealed record AnomalyAlert(
    string Title, string Detail, string Severity, double Magnitude, string? Period, string Key = "");

/// <summary>An alert as one person has it: the deviation, and whether they already saw it.</summary>
public sealed record UserAlert(AnomalyAlert Alert, bool Seen);

/// <summary>
/// The alerts of one user. The deviations are institutional and the same for everybody; what
/// each person saw or dismissed is theirs.
/// </summary>
/// <param name="Alerts">Current alerts the user has not dismissed, most urgent first.</param>
/// <param name="Dismissed">Current alerts the user dismissed, which a restore would bring back.</param>
public sealed record AlertInbox(IReadOnlyList<UserAlert> Alerts, int Dismissed)
{
    public static readonly AlertInbox Empty = new([], 0);

    public int Unseen => Alerts.Count(alert => !alert.Seen);
}

/// <summary>What a user did with one alert, as stored.</summary>
public sealed record AlertReceiptState(string Key, bool Seen, bool Dismissed);

public static class AlertInboxBuilder
{
    /// <summary>Applies one user's receipts to the alerts detected now.</summary>
    public static AlertInbox Build(IReadOnlyList<AnomalyAlert> alerts, IEnumerable<AlertReceiptState> receipts)
    {
        var byKey = new Dictionary<string, AlertReceiptState>(StringComparer.Ordinal);
        foreach (var receipt in receipts) byKey[receipt.Key] = receipt;

        var shown = new List<UserAlert>(alerts.Count);
        var dismissed = 0;

        foreach (var alert in alerts)
        {
            byKey.TryGetValue(alert.Key, out var receipt);

            // A receipt for a deviation that is no longer detected simply matches nothing.
            if (receipt is { Dismissed: true }) { dismissed++; continue; }
            shown.Add(new UserAlert(alert, receipt is { Seen: true }));
        }

        return new AlertInbox(shown, dismissed);
    }
}

/// <summary>Result of training: the public report plus every scored student.</summary>
public sealed record DropoutAnalysis(DropoutModelReport Report, IReadOnlyList<StudentRisk> Students);

/// <summary>
/// Why the active students that have no score do not have one. It used to be a single number
/// presented as "sin calificaciones", which was only one of the reasons it could be.
/// </summary>
/// <param name="NoRegistrations">Active students with no registration in the loaded periods.</param>
/// <param name="NoGrades">Active students registered but with no grade recorded yet.</param>
public sealed record UnscoredReasons(int NoRegistrations, int NoGrades)
{
    public static readonly UnscoredReasons None = new(0, 0);

    public int Total => NoRegistrations + NoGrades;
}

/// <summary>
/// The scored students and the run they came from. The summary and this list are two requests,
/// so without a shared id the screen could show the counts of one training next to the students
/// of another and present them as one picture.
/// </summary>
public sealed record StudentRiskList(string RunId, DateTime TrainedAtUtc, IReadOnlyList<StudentRisk> Students);

public class RetrainDropoutModel
{
    public required Guid RequesterKey { get; set; }
}

public class MarkAlertsSeen
{
    public required Guid RequesterKey { get; set; }
    public List<string> Keys { get; set; } = [];
}

public class DismissAlerts
{
    public required Guid RequesterKey { get; set; }
    public List<string> Keys { get; set; } = [];
}

public class RestoreAlerts
{
    public required Guid RequesterKey { get; set; }
}
