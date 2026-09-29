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
    bool HasModel)
{
    public static readonly EnrollmentForecast None = new([], [], [], [], [], 0, 0, 0, null, null, false);

    // The first projected period, when there is one.
    public string? NextLabel => HasModel && Labels.Count > HistoricalCount ? Labels[HistoricalCount] : null;
    public double? NextValue => HasModel && Actual.Count > HistoricalCount ? Actual[HistoricalCount] : null;
    public double? NextMargin => HasModel && Upper.Count > HistoricalCount
        ? Math.Round(Upper[HistoricalCount] - Trend[HistoricalCount], 1)
        : null;
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
/// <param name="Status">One line: why there is no model, or what it was trained on.</param>
/// <param name="Threshold">Probability from which a student counts as at risk.</param>
/// <param name="UnscoredActiveStudents">Active students without graded history, so not scored.</param>
public sealed record DropoutModelReport(
    bool HasModel,
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
    int UnscoredActiveStudents)
{
    public static DropoutModelReport Unavailable(
        string status, DateTime trainedAtUtc, int withdrawals, int graduates, int unscoredActive) =>
        new(false, status, trainedAtUtc, withdrawals + graduates, withdrawals, graduates,
            IntelligenceEngine.RiskThreshold, ModelQuality.None, null, [], [], [], [], 0, 0, 0, unscoredActive);
}

/// <summary>A statistically detected deviation in the institutional series.</summary>
/// <param name="Severity">info, warning or error.</param>
/// <param name="Magnitude">How far out it sits (standard deviations or points), used to rank alerts.</param>
public sealed record AnomalyAlert(string Title, string Detail, string Severity, double Magnitude, string? Period);

/// <summary>Result of training: the public report plus every scored student.</summary>
public sealed record DropoutAnalysis(DropoutModelReport Report, IReadOnlyList<StudentRisk> Students);

public class RetrainDropoutModel
{
    public required Guid RequesterKey { get; set; }
}
