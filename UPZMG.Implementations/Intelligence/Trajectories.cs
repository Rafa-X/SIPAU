using UPZMG.Shared.Enums;

namespace UPZMG.Implementations.Intelligence;

/// <summary>One student's trajectory, flattened into the dropout model's inputs.</summary>
/// <param name="PeriodsObserved">Graded periods in the feature window (at most <see cref="TrajectoryBuilder.RecentPeriods"/>).</param>
/// <param name="Retakes">Grades earned on an opportunity other than Ordinario, Título included.</param>
/// <param name="Outcome">
/// True when the student withdrew for good, false when they graduated, null while the outcome
/// is not observable yet — still enrolled, or away on a temporary leave.
/// </param>
public sealed record StudentObservation(
    Guid StudentId,
    string EnrollmentNumber,
    string FullName,
    string Career,
    int PeriodsObserved,
    double AverageGrade,
    double FailureRate,
    int Retakes,
    int EntryAge,
    bool Works,
    bool? Outcome)
{
    public double RetakesPerPeriod => PeriodsObserved == 0 ? 0 : (double)Retakes / PeriodsObserved;

    public IReadOnlyDictionary<string, double> Features => new Dictionary<string, double>
    {
        [DropoutFeatures.Average] = Math.Round(AverageGrade, 2),
        [DropoutFeatures.FailureRate] = Math.Round(FailureRate, 1),
        [DropoutFeatures.RetakesPerPeriod] = Math.Round(RetakesPerPeriod, 2),
        [DropoutFeatures.EntryAge] = EntryAge,
        [DropoutFeatures.Works] = Works ? 1 : 0,
    };
}

/// <summary>
/// Builds one observation per student with graded history.
///
/// Three leakage guards live here:
///   • Grades from the period where a trajectory ended are left out. Courses abandoned
///     when a student leaves are recorded as failures, so keeping them would teach the
///     tree that failing predicts leaving only because leaving produces the failures.
///     Graduates lose their last period too, so both classes are treated alike.
///   • Everyone is measured on the same window: the last <see cref="RecentPeriods"/> graded
///     period(s). Graduates have long records and those who leave have short ones; over the
///     whole record "never took an extraordinary exam" would mostly mean "was here briefly".
///     With a single period no trajectory can stand out by its length at all.
///   • Age is taken when the student started, not today. Finished students started
///     years ago and are older now, which would make age a proxy for "has finished".
/// </summary>
public static class TrajectoryBuilder
{
    public const int RecentPeriods = 1;

    public static IReadOnlyList<StudentObservation> Build(AcademicSnapshot snapshot) =>
        BuildWithExclusions(snapshot).Observations;

    /// <summary>
    /// The observations, plus why the active students that produced none were left out. The
    /// screens report that number, and "sin calificaciones" was only one of the two reasons.
    /// </summary>
    public static (IReadOnlyList<StudentObservation> Observations, UnscoredReasons Unscored) BuildWithExclusions(
        AcademicSnapshot snapshot)
    {
        var periodStart = snapshot.Periods.ToDictionary(p => p.Id, p => p.StartDate);

        var registrationsByStudent = snapshot.Registrations
            .Where(r => periodStart.ContainsKey(r.PeriodId))
            .GroupBy(r => r.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => periodStart[r.PeriodId]).ToList());

        var gradesByStudent = snapshot.Grades
            .Where(g => periodStart.ContainsKey(g.PeriodId))
            .GroupBy(g => g.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var observations = new List<StudentObservation>();
        int withoutRegistrations = 0, withoutGrades = 0;

        foreach (var student in snapshot.Students)
        {
            if (!registrationsByStudent.TryGetValue(student.Id, out var registrations))
            {
                if (student.Status == StudentStatus.Activo) withoutRegistrations++;
                continue;
            }

            var (outcome, endedAt) = OutcomeOf(student, registrations, periodStart);

            // A student away on a temporary leave belongs to neither population: their outcome
            // has not happened yet, so there is nothing to learn from them, and the risk list
            // is about students the areas can follow up with during this period.
            if (outcome is null && student.Status != StudentStatus.Activo) continue;

            var graded = gradesByStudent.GetValueOrDefault(student.Id, [])
                .Where(g => endedAt is null || periodStart[g.PeriodId] < endedAt)
                .ToList();

            var window = graded
                .Select(g => g.PeriodId)
                .Distinct()
                .OrderByDescending(id => periodStart[id])
                .Take(RecentPeriods)
                .ToHashSet();

            var kardex = graded.Where(g => window.Contains(g.PeriodId)).ToList();

            // Nothing graded before the outcome: there is nothing to learn from or score.
            if (kardex.Count == 0)
            {
                if (student.Status == StudentStatus.Activo) withoutGrades++;
                continue;
            }

            var entryDate = periodStart.TryGetValue(student.PeriodIdStart, out var start)
                ? start
                : periodStart[registrations[0].PeriodId];

            observations.Add(new StudentObservation(
                student.Id,
                student.EnrollmentNumber,
                student.FullName,
                student.Career,
                PeriodsObserved: window.Count,
                AverageGrade: kardex.Average(g => g.Grade),
                FailureRate: (double)kardex.Count(g => !g.IsPassed) / kardex.Count * 100,
                Retakes: kardex.Count(g => g.Opportunity != Opportunity.Ordinario),
                EntryAge: AgeAt(student.BirthDate, entryDate),
                Works: student.Works,
                Outcome: outcome));
        }

        return (observations, new UnscoredReasons(withoutRegistrations, withoutGrades));
    }

    /// <summary>
    /// The trajectory's outcome and the start of the period it happened in.
    ///
    /// Only a definitive state ends a trajectory. Taking the first state that was not "Activo"
    /// labelled a student who left for one period, came back and graduated as a withdrawal: the
    /// guard against that required the student to be enrolled *today*, so it did not protect
    /// the ones who returned and finished. A temporary leave is a pause, not an outcome.
    ///
    /// What counts as a withdrawal here is the definitive baja, which is also what the
    /// institution reports as deserción. Somebody away on a temporary leave has no observable
    /// outcome yet and is left out of training rather than counted as either class.
    /// </summary>
    private static (bool? Outcome, DateOnly? EndedAt) OutcomeOf(
        StudentRow student, List<RegistrationRow> registrations, Dictionary<Guid, DateOnly> periodStart)
    {
        var terminal = registrations
            .Where(r => r.FinalStatus is FinalStatus.Egresado or FinalStatus.BajaDefinitiva)
            .OrderBy(r => periodStart[r.PeriodId])
            // Graduating and withdrawing for good in one period cannot both have happened.
            // Graduation is the outcome that did; without a rule, the row order decided it.
            .ThenBy(r => r.FinalStatus == FinalStatus.Egresado ? 1 : 0)
            .LastOrDefault();

        if (terminal is not null)
            return (terminal.FinalStatus != FinalStatus.Egresado, periodStart[terminal.PeriodId]);

        // Nothing definitive was registered, so the student record carries the outcome.
        var lastPeriod = periodStart[registrations[^1].PeriodId];
        return student.Status switch
        {
            StudentStatus.Egresado => (false, lastPeriod),
            StudentStatus.BajaDefinitiva => (true, lastPeriod),
            _ => (null, null),
        };
    }

    public static int AgeAt(DateOnly birthDate, DateOnly date)
    {
        var age = date.Year - birthDate.Year;
        if (date < birthDate.AddYears(age)) age--;
        return Math.Max(0, age);
    }
}
