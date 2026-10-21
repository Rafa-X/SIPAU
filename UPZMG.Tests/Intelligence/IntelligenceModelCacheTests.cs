using UPZMG.Implementations.Intelligence;
using UPZMG.Persistence.Repository;
using Xunit;

namespace UPZMG.Tests.Intelligence;

/// <summary>
/// The cache the API shares between every request: many readers, one load and one training at a
/// time, and a failure that does not lock out the ones after it.
/// </summary>
public class IntelligenceModelCacheTests
{
    private static readonly AcademicFingerprint V1 = new("v1");
    private static readonly AcademicFingerprint V2 = new("v2");

    /// <summary>A fake data source that counts its loads and trainings and takes some time over each.</summary>
    private sealed class Source
    {
        private int _loads, _trainings, _runs;

        public int Loads => _loads;
        public int Trainings => _trainings;
        public int FailNextTrainings { get; set; }

        public async Task<IntelligenceState> Load(AcademicFingerprint fingerprint)
        {
            Interlocked.Increment(ref _loads);
            await Task.Delay(40);
            var run = $"run{Interlocked.Increment(ref _runs)}";
            return new IntelligenceState(fingerprint, DateTime.UtcNow, run, AcademicSnapshot.Empty, AcademicHistory.Empty);
        }

        public DropoutAnalysis Train(IntelligenceState state)
        {
            Interlocked.Increment(ref _trainings);
            Thread.Sleep(40);

            if (FailNextTrainings > 0)
            {
                FailNextTrainings--;
                throw new InvalidOperationException("training failed");
            }

            var report = DropoutModelReport.Unavailable("test", state.RunId, DateTime.UtcNow, 0, 0, UnscoredReasons.None);
            return new DropoutAnalysis(report, []);
        }
    }

    private static Task<(IntelligenceState State, DropoutAnalysis Analysis)[]> Parallel(
        IntelligenceModelCache cache, Source source, AcademicFingerprint fingerprint, int requests, bool force = false) =>
        Task.WhenAll(Enumerable.Range(0, requests)
            .Select(_ => Task.Run(() => cache.GetModelAsync(fingerprint, source.Load, source.Train, force))));

    [Fact]
    public async Task ParallelRequestsShareOneLoadAndOneTraining()
    {
        var cache = new IntelligenceModelCache();
        var source = new Source();

        var results = await Parallel(cache, source, V1, requests: 24);

        Assert.Equal(1, source.Loads);
        Assert.Equal(1, source.Trainings);
        Assert.All(results, r => Assert.Same(results[0].Analysis, r.Analysis));
    }

    [Fact]
    public async Task EveryAnswerPairsTheDataWithTheModelTrainedOnIt()
    {
        var cache = new IntelligenceModelCache();
        var source = new Source();

        // Readers and two forced retrainings at the same time, over a change of data.
        var readers = Parallel(cache, source, V1, requests: 16);
        var retrain = Parallel(cache, source, V1, requests: 2, force: true);
        var changed = Parallel(cache, source, V2, requests: 8);

        var all = (await readers).Concat(await retrain).Concat(await changed).ToList();

        // A run id names one cut of the data: the summary and the student list of an answer
        // must come from the same one, whatever else was happening.
        Assert.All(all, r => Assert.Equal(r.State.RunId, r.Analysis.Report.RunId));
        Assert.True(source.Trainings <= source.Loads, $"{source.Trainings} trainings for {source.Loads} loads");
    }

    [Fact]
    public async Task ChangedDataIsReloadedAndRetrained()
    {
        var cache = new IntelligenceModelCache();
        var source = new Source();

        var before = await cache.GetModelAsync(V1, source.Load, source.Train, force: false);
        var again = await cache.GetModelAsync(V1, source.Load, source.Train, force: false);
        var after = await cache.GetModelAsync(V2, source.Load, source.Train, force: false);

        Assert.Same(before.Analysis, again.Analysis);
        Assert.NotEqual(before.State.RunId, after.State.RunId);
        Assert.Equal(2, source.Loads);
        Assert.Equal(2, source.Trainings);
    }

    [Fact]
    public async Task AFailedTrainingDoesNotLockOutTheNextOne()
    {
        var cache = new IntelligenceModelCache();
        var source = new Source { FailNextTrainings = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.GetModelAsync(V1, source.Load, source.Train, force: false));

        // The gate was released and nothing half-built was kept: the next request trains and answers.
        var recovered = await cache.GetModelAsync(V1, source.Load, source.Train, force: false)
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(recovered.State.RunId, recovered.Analysis.Report.RunId);
        Assert.Equal(2, source.Trainings);
    }

    [Fact]
    public async Task AForcedRetrainingReplacesTheModelForEveryoneAfterIt()
    {
        var cache = new IntelligenceModelCache();
        var source = new Source();

        var first = await cache.GetModelAsync(V1, source.Load, source.Train, force: false);
        var forced = await cache.GetModelAsync(V1, source.Load, source.Train, force: true);
        var next = await cache.GetModelAsync(V1, source.Load, source.Train, force: false);

        Assert.NotSame(first.Analysis, forced.Analysis);
        Assert.Same(forced.Analysis, next.Analysis);
        Assert.Equal(2, source.Trainings);
    }
}
