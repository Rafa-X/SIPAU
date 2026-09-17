using UPZMG.Shared.Enums;

namespace UPZMG.Implementations.Intelligence;

// Plain rows read from the database. The builders below turn them into model input,
// so all the feature engineering can be tested without a database.

/// <param name="Label">How the period is shown, e.g. "2025 Sep-Dic".</param>
public sealed record PeriodRow(Guid Id, string Label, DateOnly StartDate);

public sealed record StudentRow(
    Guid Id,
    string EnrollmentNumber,
    string FullName,
    string Career,
    Guid PeriodIdStart,
    DateOnly BirthDate,
    bool Works,
    StudentStatus Status);

public sealed record RegistrationRow(Guid StudentId, Guid PeriodId, FinalStatus FinalStatus);

public sealed record GradeRow(Guid StudentId, Guid PeriodId, double Grade, bool IsPassed, Opportunity Opportunity);

/// <summary>Everything the intelligent layer reads, loaded once per training.</summary>
public sealed record AcademicSnapshot(
    IReadOnlyList<PeriodRow> Periods,
    IReadOnlyList<StudentRow> Students,
    IReadOnlyList<RegistrationRow> Registrations,
    IReadOnlyList<GradeRow> Grades)
{
    public static readonly AcademicSnapshot Empty = new([], [], [], []);
}
