using UPZMG.Implementations.Intelligence;
using Xunit;

namespace UPZMG.Tests.Intelligence;

// The regression behind every projection, checked against values worked out by hand.
public class StatisticsTests
{
    [Fact]
    public void Fit_RecoversAPerfectLineExactly()
    {
        // y = 100 + 50x
        var fit = Statistics.Fit([100, 150, 200, 250, 300]);

        Assert.NotNull(fit);
        Assert.Equal(50, fit.Slope, precision: 6);
        Assert.Equal(100, fit.Intercept, precision: 6);
        Assert.Equal(1.0, fit.R2, precision: 6);
        Assert.Equal(0, fit.StdError, precision: 6);
    }

    [Fact]
    public void Fit_MatchesAHandComputedExample()
    {
        // x = 0..3, y = 2, 4, 5, 8 → b = 9.5 / 5 = 1.9, a = 4.75 − 1.9·1.5 = 1.9, SSres = 0.7, SStot = 18.75
        var fit = Statistics.Fit([2, 4, 5, 8])!;

        Assert.Equal(1.9, fit.Slope, precision: 9);
        Assert.Equal(1.9, fit.Intercept, precision: 9);
        Assert.Equal(1 - 0.7 / 18.75, fit.R2, precision: 9);
        Assert.Equal(Math.Sqrt(0.7 / 2), fit.StdError, precision: 9);
    }

    [Fact]
    public void Fit_ProjectsBeyondTheObservedRange()
    {
        var fit = Statistics.Fit([10, 20, 30, 40])!;

        Assert.Equal(50, fit.PredictAt(4), precision: 6);
        Assert.Equal(60, fit.PredictAt(5), precision: 6);
    }

    [Fact]
    public void Fit_ReportsLowerR2ForNoisierData()
    {
        var clean = Statistics.Fit([10, 20, 30, 40, 50])!;
        var noisy = Statistics.Fit([10, 35, 22, 47, 31])!;

        Assert.True(clean.R2 > noisy.R2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Fit_RefusesFewerThanTwoPoints(int count)
    {
        var y = Enumerable.Range(1, count).Select(i => (double)i).ToList();

        Assert.Null(Statistics.Fit(y));
    }

    [Fact]
    public void Fit_AllowsTwoPointsForAnInitialProjection()
    {
        var fit = Statistics.Fit([500, 520]);

        Assert.NotNull(fit);
        Assert.Equal(20, fit.Slope);
        Assert.Equal(0, fit.StdError);
    }

    [Fact]
    public void Fit_TreatsAFlatSeriesAsPerfectlyExplained()
    {
        var fit = Statistics.Fit([500, 500, 500, 500])!;

        Assert.Equal(0, fit.Slope, precision: 6);
        Assert.Equal(1.0, fit.R2, precision: 6);
    }

    [Fact]
    public void ZScores_AreZeroForAConstantSeries()
    {
        var z = Statistics.ZScores([7, 7, 7, 7]);

        Assert.All(z, value => Assert.Equal(0, value, precision: 9));
    }

    [Fact]
    public void ZScores_FlagTheOutlierAndOnlyTheOutlier()
    {
        var z = Statistics.ZScores([10, 10, 10, 10, 40]);

        Assert.True(Math.Abs(z[4]) > 1.8);
        Assert.All(z.Take(4), value => Assert.True(Math.Abs(value) < 1.8));
    }

    [Fact]
    public void StandardDeviation_IsThePopulationOne()
    {
        // mean 5, squared deviations 9+1+1+9 = 20, /4 = 5
        Assert.Equal(Math.Sqrt(5), Statistics.StandardDeviation([2, 4, 6, 8]), precision: 9);
        Assert.Equal(0, Statistics.StandardDeviation([]), precision: 9);
    }

    [Fact]
    public void PercentChange_StartsAtZeroAndComputesRelativeSteps()
    {
        var change = Statistics.PercentChange([100, 150, 75]);

        Assert.Equal(new double[] { 0, 50, -50 }, change);
    }

    [Fact]
    public void PercentChange_DoesNotDivideByZero()
    {
        Assert.Equal(0, Statistics.PercentChange([0, 100])[1], precision: 6);
    }
}
