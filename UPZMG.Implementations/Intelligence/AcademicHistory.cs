using System.Globalization;
using UPZMG.Shared.Enums;

namespace UPZMG.Implementations.Intelligence;

/// <summary>Enrolment, pass rate and outcomes of one period that has registrations.</summary>
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
    public double PassRate => Passed + Failed == 0 ? 0 : (double)Passed / (Passed + Failed) * 100;
    public double WithdrawalRate => Enrolled == 0 ? 0 : (double)Withdrawn / Enrolled * 100;
}

/// <summary>The institution's observed periods, oldest first, plus the known periods still ahead.</summary>
public sealed record AcademicHistory(IReadOnlyList<PeriodStats> Periods, IReadOnlyList<string> UpcomingLabels)
{
    public static readonly AcademicHistory Empty = new([], []);

    // Three points is the least a regression can be fitted to.
    public bool CanForecast => Periods.Count >= 3;
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

        var firstEnrolments = CountFirstEnrolments(
            snapshot.Registrations.Select(r => (r.StudentId, r.PeriodId)),
            [.. ordered.Select(p => p.Id)]);

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
                NewEnrolled: firstEnrolments.GetValueOrDefault(period.Id),
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
    /// Students enrolling for the first time in each period. A student seen in periods
    /// 3, 4 and 5 counts once, in period 3.
    /// </summary>
    /// <param name="periodOrder">Period ids in chronological order.</param>
    public static Dictionary<Guid, int> CountFirstEnrolments(
        IEnumerable<(Guid StudentId, Guid PeriodId)> enrolments,
        IReadOnlyList<Guid> periodOrder)
    {
        var rank = new Dictionary<Guid, int>(periodOrder.Count);
        for (int i = 0; i < periodOrder.Count; i++) rank[periodOrder[i]] = i;

        var firstByStudent = new Dictionary<Guid, Guid>();
        foreach (var (studentId, periodId) in enrolments)
        {
            // An unknown period cannot be ranked; skipping it beats guessing.
            if (!rank.TryGetValue(periodId, out var r)) continue;

            if (!firstByStudent.TryGetValue(studentId, out var current) || r < rank[current])
                firstByStudent[studentId] = periodId;
        }

        return firstByStudent.Values
            .GroupBy(p => p)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>The period after a "2025 Sep-Dic" style label; null for any other shape.</summary>
    public static string? NextLabel(string label)
    {
        var parts = label.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            return null;
        }

        var term = Array.IndexOf(Terms, parts[1]);
        if (term < 0) return null;

        return term == Terms.Length - 1
            ? $"{year + 1} {Terms[0]}"
            : $"{year} {Terms[term + 1]}";
    }

    private static int DistinctStudents(List<RegistrationRow> rows, params FinalStatus[] statuses) =>
        rows.Where(r => statuses.Contains(r.FinalStatus)).Select(r => r.StudentId).Distinct().Count();
}
