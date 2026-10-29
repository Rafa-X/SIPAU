using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// A checksum of everything the intelligent layer reads, used to notice that the model is stale.
///
/// It used to be row counts plus two sums, which missed most edits: correcting a birth date, a
/// career, an entry period or the opportunity of a grade left the fingerprint identical, so the
/// model went on answering with the old data for up to an hour. CHECKSUM_AGG over
/// BINARY_CHECKSUM(*) changes when any column of any row changes, and compensating edits that
/// cancelled each other out no longer go unnoticed.
/// </summary>
public sealed record AcademicFingerprint(string Value)
{
    public static readonly AcademicFingerprint Empty = new(string.Empty);
}

/// <summary>
/// The data one load produced: the snapshot itself and the indicators derived from it. The
/// dropout model is not part of it — training is the expensive step and only the screens that
/// show risk need it.
/// </summary>
/// <param name="RunId">
/// Identifies this cut of the data. The report and the student list carry it so the screen can
/// tell that it is reading two different runs instead of showing them side by side as one.
/// </param>
public sealed record IntelligenceState(
    AcademicFingerprint Fingerprint,
    DateTime BuiltAtUtc,
    string RunId,
    AcademicSnapshot Snapshot,
    AcademicHistory History);

/// <summary>
/// Keeps the last load and the last training between requests (registered as a singleton).
/// Both are rebuilt when the data fingerprint changes, when they are an hour old, or on demand.
///
/// They are kept apart on purpose. The notification bell in the layout asks for anomalies on
/// every page, and with a single cache that request paid for the decision tree and its
/// five-fold validation as well.
/// </summary>
public sealed class IntelligenceModelCache
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    private readonly SemaphoreSlim _dataGate = new(1, 1);
    private readonly SemaphoreSlim _modelGate = new(1, 1);

    // Both are replaced whole, never edited, so a request that reads them without taking a gate
    // sees either the previous object or the new one. The model used to be a tuple: a struct
    // that a reader could catch half written, with the run id of one training next to the
    // analysis of another.
    private volatile IntelligenceState? _state;
    private volatile TrainedModel? _model;

    // Numbers the loads: it turns odd when one starts and even when it ends, and _stateTicket is
    // the number the current state's load started with. A forced request is answered by a load
    // that was running when it arrived or started after it: either one read the data no earlier
    // than the press. Without this every "Reentrenar" reloaded the data on its own, each reload
    // was a new run, and eight presses at the same moment trained up to eight times in a row.
    private long _loadTicket;
    private long _stateTicket;

    private sealed record TrainedModel(string RunId, DropoutAnalysis Analysis);

    public async Task<IntelligenceState> GetStateAsync(
        AcademicFingerprint fingerprint, Func<AcademicFingerprint, Task<IntelligenceState>> load, bool force)
    {
        if (!force && IsFresh(_state, fingerprint)) return _state!;

        var arrival = Interlocked.Read(ref _loadTicket);

        await _dataGate.WaitAsync();
        try
        {
            // Another request may have rebuilt it while this one waited.
            if (!force && IsFresh(_state, fingerprint)) return _state!;
            if (force && _state is { } shared && Interlocked.Read(ref _stateTicket) >= arrival) return shared;

            var ticket = Interlocked.Increment(ref _loadTicket);
            try
            {
                var state = await load(fingerprint);
                _state = state;
                Interlocked.Exchange(ref _stateTicket, ticket);
                return state;
            }
            finally
            {
                Interlocked.Increment(ref _loadTicket);
            }
        }
        finally
        {
            _dataGate.Release();
        }
    }

    /// <summary>The trained model for the current data, training it only if nothing else has.</summary>
    public async Task<(IntelligenceState State, DropoutAnalysis Analysis)> GetModelAsync(
        AcademicFingerprint fingerprint,
        Func<AcademicFingerprint, Task<IntelligenceState>> load,
        Func<IntelligenceState, DropoutAnalysis> train,
        bool force)
    {
        var state = await GetStateAsync(fingerprint, load, force);

        if (!force && _model is { } cached && cached.RunId == state.RunId) return (state, cached.Analysis);

        // One training at a time; whoever waits gets the result of the one before when it fits.
        await _modelGate.WaitAsync();
        try
        {
            // Train on the newest cut of the data, not on the one this request read before it
            // waited. A forced reload may have replaced it meanwhile, and training on the older
            // cut put the cache back on data it had already moved past.
            var latest = _state ?? state;

            // A model of that cut answers forced requests too: each one reloaded the data on its
            // way here, so a model of the newest cut was trained after it was asked for. Two people
            // pressing "Reentrenar" at once get one training, not the same one twice in a row.
            if (_model is { } current && current.RunId == latest.RunId) return (latest, current.Analysis);

            var analysis = train(latest);
            _model = new TrainedModel(latest.RunId, analysis);
            return (latest, analysis);
        }
        finally
        {
            // Released even when training throws: a failed run must not lock out every later one.
            _modelGate.Release();
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
        (await GetModelAsync()).Analysis.Report;

    /// <summary>Every active student with a score, highest risk first, and which run produced them.</summary>
    public async Task<StudentRiskList> GetStudentRisksAsync()
    {
        var (state, analysis) = await GetModelAsync();
        return new StudentRiskList(state.RunId, analysis.Report.TrainedAtUtc, analysis.Students);
    }

    // Forces a new training even if the data did not change
    public async Task<DropoutModelReport> RetrainAsync() =>
        (await GetModelAsync(force: true)).Analysis.Report;

    private async Task<IntelligenceState> GetStateAsync(bool force = false) =>
        await cache.GetStateAsync(await GetFingerprintAsync(), LoadAsync, force);

    private async Task<(IntelligenceState State, DropoutAnalysis Analysis)> GetModelAsync(bool force = false) =>
        await cache.GetModelAsync(await GetFingerprintAsync(), LoadAsync, Train, force);

    /// <summary>
    /// Reads the academic data once and derives the indicators from it. The snapshot is kept so
    /// that asking for risk afterwards does not read 30,000 grades again.
    /// </summary>
    private async Task<IntelligenceState> LoadAsync(AcademicFingerprint fingerprint)
    {
        var snapshot = await LoadSnapshotAsync();

        return new IntelligenceState(
            fingerprint,
            DateTime.UtcNow,
            RunId: Guid.NewGuid().ToString("N")[..8],
            snapshot,
            AcademicHistoryBuilder.Build(snapshot));
    }

    private static DropoutAnalysis Train(IntelligenceState state)
    {
        var (observations, unscored) = TrajectoryBuilder.BuildWithExclusions(state.Snapshot);
        return IntelligenceEngine.AnalyseDropout(observations, unscored, state.RunId, DateTime.UtcNow);
    }

    /// <summary>
    /// One round trip, one scalar: the row count and a content checksum of each table the
    /// snapshot reads. Any edit to any of their columns changes the value.
    /// </summary>
    private async Task<AcademicFingerprint> GetFingerprintAsync()
    {
        var value = await db.Database
            .SqlQuery<string>($"""
                SELECT CONCAT(
                    (SELECT COUNT_BIG(*) FROM periods), ':',
                    (SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)), 0) FROM periods), '|',
                    (SELECT COUNT_BIG(*) FROM students), ':',
                    (SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)), 0) FROM students), '|',
                    (SELECT COUNT_BIG(*) FROM courseEnrollment), ':',
                    (SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)), 0) FROM courseEnrollment), '|',
                    (SELECT COUNT_BIG(*) FROM kardex), ':',
                    (SELECT ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(*)), 0) FROM kardex)) AS Value
                """)
            // Exactly one row by construction. First without an order made EF log a warning
            // on every check, and the check runs on every request to the model.
            .SingleOrDefaultAsync();

        return string.IsNullOrEmpty(value) ? AcademicFingerprint.Empty : new AcademicFingerprint(value);
    }

    private async Task<AcademicSnapshot> LoadSnapshotAsync()
    {
        // Read without a transaction, on purpose. A consistent read of these four tables needs
        // snapshot isolation, which is off on this database and cannot be turned on by the
        // application's login; the alternative, a repeatable-read transaction, would hold shared
        // locks over 60,000 rows and block the capture screens while the model loads. A capture
        // landing mid-read can leave a grade whose student is not in this snapshot, and both
        // builders ignore rows they cannot place, so the figures lag by one load instead of
        // describing a state that never existed. Enabling it is one statement for a DBA:
        //   ALTER DATABASE upzmg SET ALLOW_SNAPSHOT_ISOLATION ON;
        var periods = await db.Periods
            .AsNoTracking()
            .OrderBy(p => p.StartDate)
            .Select(p => new PeriodRow(p.Id, p.SchoolCycle + " " + p.SEP_Period, p.StartDate))
            .ToListAsync();

        var careers = await db.Careers
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name ?? "Sin nombre");

        // In a stated order: the five validation folds are dealt from this list, so an order left
        // to the query plan could change the threshold the screen publishes. By id it is the order
        // the database already returns them in, and the figures stay the ones reported.
        var students = (await db.Students
                .AsNoTracking()
                .OrderBy(s => s.Id)
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
