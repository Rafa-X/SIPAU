using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

/// <summary>Cheap summary of the academic data; when it changes, the model is retrained.</summary>
public sealed record AcademicFingerprint(
    int Periods,
    int Students,
    int StudentStatusSum,
    int WorkingStudents,
    int Registrations,
    int FinalStatusSum,
    int Grades,
    double GradeSum)
{
    public static readonly AcademicFingerprint Empty = new(0, 0, 0, 0, 0, 0, 0, 0);
}

/// <summary>What one training produced: the history for projections and the dropout analysis.</summary>
public sealed record IntelligenceState(
    AcademicFingerprint Fingerprint,
    DateTime BuiltAtUtc,
    AcademicHistory History,
    DropoutAnalysis Dropout);

/// <summary>
/// Keeps the last training between requests (registered as a singleton). The model is
/// rebuilt when the data fingerprint changes, when it is an hour old, or on demand.
/// </summary>
public sealed class IntelligenceModelCache
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IntelligenceState? _state;

    public async Task<IntelligenceState> GetAsync(
        AcademicFingerprint fingerprint, Func<AcademicFingerprint, Task<IntelligenceState>> build, bool force)
    {
        if (!force && IsFresh(_state, fingerprint)) return _state!;

        await _gate.WaitAsync();
        try
        {
            // Another request may have rebuilt it while this one waited.
            if (!force && IsFresh(_state, fingerprint)) return _state!;

            _state = await build(fingerprint);
            return _state;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool IsFresh(IntelligenceState? state, AcademicFingerprint fingerprint) =>
        state is not null
        && state.Fingerprint == fingerprint
        && DateTime.UtcNow - state.BuiltAtUtc < MaxAge;
}

public class IntelligenceRepository(AppDBContext db, IntelligenceModelCache cache)
{
    // Enrolment projection over every period that has registrations
    public async Task<EnrollmentForecast> GetForecastAsync(int periodsAhead) =>
        IntelligenceEngine.Forecast((await GetStateAsync()).History, periodsAhead);

    // Enrolment, pass rate and outcomes of every period that has registrations
    public async Task<AcademicHistory> GetHistoryAsync() => (await GetStateAsync()).History;

    // Statistical alerts over the same history
    public async Task<IReadOnlyList<AnomalyAlert>> GetAnomaliesAsync() =>
        IntelligenceEngine.DetectAnomalies((await GetStateAsync()).History);

    // Model quality, learned rules and the most urgent students
    public async Task<DropoutModelReport> GetDropoutReportAsync() =>
        (await GetStateAsync()).Dropout.Report;

    // Every active student with a score, highest risk first
    public async Task<IReadOnlyList<StudentRisk>> GetStudentRisksAsync() =>
        (await GetStateAsync()).Dropout.Students;

    // Forces a new training even if the data did not change
    public async Task<DropoutModelReport> RetrainAsync() =>
        (await GetStateAsync(force: true)).Dropout.Report;

    private async Task<IntelligenceState> GetStateAsync(bool force = false)
    {
        var fingerprint = await GetFingerprintAsync();
        return await cache.GetAsync(fingerprint, BuildAsync, force);
    }

    private async Task<IntelligenceState> BuildAsync(AcademicFingerprint fingerprint)
    {
        var snapshot = await LoadSnapshotAsync();

        var observations = TrajectoryBuilder.Build(snapshot);
        var scoredIds = observations.Where(o => o.Outcome is null).Select(o => o.StudentId).ToHashSet();
        var unscoredActive = snapshot.Students.Count(s => s.Status == StudentStatus.Activo && !scoredIds.Contains(s.Id));

        var builtAt = DateTime.UtcNow;
        return new IntelligenceState(
            fingerprint,
            builtAt,
            AcademicHistoryBuilder.Build(snapshot),
            IntelligenceEngine.AnalyseDropout(observations, unscoredActive, builtAt));
    }

    // One round trip; sums catch edits that keep the row counts unchanged.
    private async Task<AcademicFingerprint> GetFingerprintAsync() =>
        await db.Periods
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new AcademicFingerprint(
                g.Count(),
                db.Students.Count(),
                db.Students.Sum(s => (int?)s.Status) ?? 0,
                db.Students.Count(s => s.Works),
                db.CourseEnrollment.Count(),
                db.CourseEnrollment.Sum(r => (int?)r.FinalStatus) ?? 0,
                db.Kardexes.Count(),
                db.Kardexes.Sum(k => (double?)k.Grade) ?? 0))
            .FirstOrDefaultAsync()
        ?? AcademicFingerprint.Empty;

    private async Task<AcademicSnapshot> LoadSnapshotAsync()
    {
        var periods = await db.Periods
            .AsNoTracking()
            .OrderBy(p => p.StartDate)
            .Select(p => new PeriodRow(p.Id, p.SchoolCycle + " " + p.SEP_Period, p.StartDate))
            .ToListAsync();

        var careers = await db.Careers
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name ?? "Sin nombre");

        var students = (await db.Students
                .AsNoTracking()
                .Select(s => new
                {
                    s.Id, s.EnrollmentNumber, s.Name, s.MiddleName, s.LastName,
                    s.CareerId, s.PeriodIdStart, s.BirthDate, s.Works, s.Status,
                })
                .ToListAsync())
            .Select(s => new StudentRow(
                s.Id,
                s.EnrollmentNumber,
                string.Join(' ', new[] { s.Name, s.MiddleName, s.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                careers.GetValueOrDefault(s.CareerId, "Sin carrera"),
                s.PeriodIdStart,
                s.BirthDate,
                s.Works,
                s.Status))
            .ToList();

        // One row per subject in the table; the model only needs one per student and period.
        var registrations = await db.CourseEnrollment
            .AsNoTracking()
            .Select(r => new { r.StudentId, r.PeriodId, r.FinalStatus })
            .Distinct()
            .ToListAsync();

        var grades = await db.Kardexes
            .AsNoTracking()
            .Select(k => new GradeRow(k.StudentId, k.PeriodId, k.Grade, k.IsPassed, k.Opportunity))
            .ToListAsync();

        return new AcademicSnapshot(
            periods,
            students,
            [.. registrations.Select(r => new RegistrationRow(r.StudentId, r.PeriodId, r.FinalStatus))],
            grades);
    }
}
