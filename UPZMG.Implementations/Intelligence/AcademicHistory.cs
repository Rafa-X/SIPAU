using System.Globalization;
using UPZMG.Shared.Enums;

namespace UPZMG.Implementations.Intelligence;

/// <summary>Enrolment, pass rate and outcomes of one period that has registrations.</summary>
/// <param name="NewEnrolled">Students whose record says they entered in this period.</param>
/// <param name="Passed">Graded attempts that passed. Attempts, not subjects: a subject taken
/// twice counts twice, which is what the kardex records.</param>
public sealed record PeriodStats(
    Guid PeriodId,
    string Label,
    DateOnly StartDate,
    int Enrolled,
    int NewEnrolled,
    int Withdrawn,
    int Graduated,
    int Passed,
    int Failed)
{
    /// <summary>True when the period has grades, so a rate can be computed at all.</summary>
    public bool HasGrades => Passed + Failed > 0;

    /// <summary>
    /// Share of graded attempts that passed, or null when nothing has been graded yet.
    /// Reporting zero made every screen that draws the complement show a period with no kardex
    /// as 100 % failure, which is a statement about the students and not about the data.
    /// </summary>
    public double? PassRate => HasGrades ? (double)Passed / (Passed + Failed) * 100 : null;

    public double WithdrawalRate => Enrolled == 0 ? 0 : (double)Withdrawn / Enrolled * 100;
}

/// <summary>The institution's observed periods, oldest first, plus the known periods still ahead.</summary>
public sealed record AcademicHistory(IReadOnlyList<PeriodStats> Periods, IReadOnlyList<string> UpcomingLabels)
{
    public static readonly AcademicHistory Empty = new([], []);

    // Two points define the first usable trend for an enrolment projection.
    public bool CanForecast => Periods.Count >= 2;
}

public static class AcademicHistoryBuilder
{
    private static readonly string[] Terms = ["Ene-Abr", "May-Ago", "Sep-Dic"];

    public static AcademicHistory Build(AcademicSnapshot snapshot)
    {
        var ordered = snapshot.Periods.OrderBy(p => p.StartDate).ToList();

        var registrationsByPeriod = snapshot.Registrations
            .GroupBy(r => r.PeriodId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var gradesByPeriod = snapshot.Grades
            .GroupBy(g => g.PeriodId)
            .ToDictionary(g => g.Key, g => (Passed: g.Count(x => x.IsPassed), Failed: g.Count(x => !x.IsPassed)));

        var entrants = CountEntrants(snapshot.Students, [.. ordered.Select(p => p.Id)]);

        var stats = new List<PeriodStats>();

        foreach (var period in ordered)
        {
            // A period nobody registered in has not happened yet (or was never loaded);
            // counting it as zero would forecast a collapse that is only an artefact.
            if (!registrationsByPeriod.TryGetValue(period.Id, out var rows)) continue;

            // Students, not rows: one student takes several subjects.
            var grades = gradesByPeriod.GetValueOrDefault(period.Id);
            stats.Add(new PeriodStats(
                period.Id,
                period.Label,
                period.StartDate,
                Enrolled: rows.Select(r => r.StudentId).Distinct().Count(),
                NewEnrolled: entrants.GetValueOrDefault(period.Id),
                Withdrawn: DistinctStudents(rows, FinalStatus.BajaTemporal, FinalStatus.BajaDefinitiva),
                Graduated: DistinctStudents(rows, FinalStatus.Egresado),
                Passed: grades.Passed,
                Failed: grades.Failed));
        }

        var upcoming = stats.Count == 0
            ? []
            : ordered
                .Where(p => p.StartDate > stats[^1].StartDate && !registrationsByPeriod.ContainsKey(p.Id))
                .Select(p => p.Label)
                .ToList();

        return new AcademicHistory(stats, upcoming);
    }

    /// <summary>
    /// New entrants per period, taken from the entry period each student record declares.
    ///
    /// It used to be the earliest enrolment the data showed, which turns the first period of
    /// the loaded window into an enormous intake: everybody already studying appeared to be
    /// entering there. A student whose entry period is not among the loaded ones entered
    /// before this window and is not an entrant in any of these periods.
    /// </summary>
    /// <param name="periodOrder">Period ids in chronological order.</param>
    public static Dictionary<Guid, int> CountEntrants(
        IEnumerable<StudentRow> students,
        IReadOnlyList<Guid> periodOrder)
    {
        var known = periodOrder.ToHashSet();

        return students
            .Where(s => known.Contains(s.PeriodIdStart))
            .GroupBy(s => s.PeriodIdStart)
            .ToDictionary(g => g.Key, g => g.Select(s => s.Id).Distinct().Count());
    }

    /// <summary>The period after a "2025 Sep-Dic" style label; null for any other shape.</summary>
    public static string? NextLabel(string label)
    {
        if (Parse(label) is not var (year, term)) return null;

        return term == Terms.Length - 1
            ? $"{year + 1} {Terms[0]}"
            : $"{year} {Terms[term + 1]}";
    }

    /// <summary>
    /// How many terms a label sits from the start of the calendar; null for any other shape.
    /// It lets the projection measure its x in periods rather than in observations, so a term
    /// the university did not run is a gap in the series and not a shorter step.
    /// </summary>
    public static int? TermOrdinal(string label) =>
        Parse(label) is var (year, term) ? year * Terms.Length + term : null;

    private static (int Year, int Term)? Parse(string label)
    {
        var parts = label.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            return null;
        }

        var term = Array.IndexOf(Terms, parts[1]);
        return term < 0 ? null : (year, term);
    }

    private static int DistinctStudents(List<RegistrationRow> rows, params FinalStatus[] statuses) =>
        rows.Where(r => statuses.Contains(r.FinalStatus)).Select(r => r.StudentId).Distinct().Count();
}
