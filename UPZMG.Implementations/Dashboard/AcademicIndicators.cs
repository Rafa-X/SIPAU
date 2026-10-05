namespace UPZMG.Implementations.Dashboard;

/// <summary>
/// The three indicators the university reports to the SII, computed with the formulas it gave us
/// rather than captured by hand. None of them is a pass rate: aprovechamiento is the average
/// grade, and the university was explicit about the difference.
/// </summary>
/// <param name="PeriodLabel">Period the first two indicators describe.</param>
/// <param name="Achievement">Average grade of the period, 0–10. Null when nothing was graded.</param>
/// <param name="DropoutRate">Definitive withdrawals ÷ students enrolled in the period, as a percentage.</param>
/// <param name="TerminalEfficiency">Graduates ÷ entrants of the most recent finished cohort, as a percentage.</param>
/// <param name="CohortLabel">Entry period of that cohort; null when no cohort has finished yet.</param>
public sealed record AcademicIndicators(
    string? PeriodLabel,
    double? Achievement,
    double? DropoutRate,
    int WithdrawnStudents,
    int EnrolledStudents,
    double? TerminalEfficiency,
    string? CohortLabel,
    int CohortGraduates,
    int CohortEntrants,
    IReadOnlyList<CareerAchievement> ByCareer)
{
    public static readonly AcademicIndicators None =
        new(null, null, null, 0, 0, null, null, 0, 0, []);

    public bool HasData => Achievement is not null || DropoutRate is not null || TerminalEfficiency is not null;
}

/// <summary>Average grade of one programme in the period, so a low overall figure can be located.</summary>
public sealed record CareerAchievement(string Career, double Achievement, int GradedStudents);
