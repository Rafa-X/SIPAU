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
        Assert.Equal(1.0, fit.R2!.Value, precision: 6);
        Assert.Equal(0, fit.StdError!.Value, precision: 6);
    }

    [Fact]
    public void Fit_MatchesAHandComputedExample()
    {
        // x = 0..3, y = 2, 4, 5, 8 → b = 9.5 / 5 = 1.9, a = 4.75 − 1.9·1.5 = 1.9, SSres = 0.7, SStot = 18.75
        var fit = Statistics.Fit([2, 4, 5, 8])!;

        Assert.Equal(1.9, fit.Slope, precision: 9);
        Assert.Equal(1.9, fit.Intercept, precision: 9);
        Assert.Equal(1 - 0.7 / 18.75, fit.R2!.Value, precision: 9);
        Assert.Equal(Math.Sqrt(0.7 / 2), fit.StdError!.Value, precision: 9);
    }

    [Fact]
    public void PredictionMargin_MatchesAHandComputedExample()
    {
        // x = 0..4, y = 10, 12, 15, 15, 19 → b = 2.1, a = 10, SSres = 2.7, s = √0.9, x̄ = 2, Sxx = 10.
        // At x = 5: t(0.975, 3) · s · √(1 + 1/5 + 9/10) = 3.182 · 0.9487 · 1.4491.
        var fit = Statistics.Fit([10, 12, 15, 15, 19])!;

        Assert.Equal(2.1, fit.Slope, precision: 9);
        Assert.Equal(3.182 * Math.Sqrt(0.9) * Math.Sqrt(2.1), fit.PredictionMargin(5)!.Value, precision: 9);

        // Narrowest at the centre of the data, wider at its ends.
        Assert.True(fit.PredictionMargin(2) < fit.PredictionMargin(0));
        Assert.Equal(fit.PredictionMargin(0)!.Value, fit.PredictionMargin(4)!.Value, precision: 9);
    }

    [Fact]
    public void PredictionMargin_IsUnknownWithTwoPoints()
    {
        Assert.Null(Statistics.Fit([500, 520])!.PredictionMargin(2));
    }

    [Theory]
    [InlineData(1, 12.706)]
    [InlineData(3, 3.182)]
    [InlineData(8, 2.306)]
    [InlineData(30, 2.042)]
    [InlineData(60, 2.000)]
    [InlineData(120, 1.980)]
    public void StudentT975_MatchesTheTable(int degreesOfFreedom, double expected)
    {
        Assert.Equal(expected, Statistics.StudentT975(degreesOfFreedom), precision: 3);
    }

    [Theory]
    [InlineData(0, 10, 0.0, 0.2775)]
    [InlineData(5, 10, 0.2366, 0.7634)]
    [InlineData(10, 10, 0.7225, 1.0)]
    public void Wilson_MatchesTheTextbookInterval(int successes, int total, double lower, double upper)
    {
        var interval = Statistics.Wilson(successes, total);

        Assert.Equal(lower, interval.Lower, precision: 4);
        Assert.Equal(upper, interval.Upper, precision: 4);
    }

    [Fact]
    public void Wilson_NarrowsAsTheSampleGrows()
    {
        var small = Statistics.Wilson(3, 10);
        var large = Statistics.Wilson(300, 1000);

        Assert.True(large.Upper - large.Lower < small.Upper - small.Lower);
        Assert.Equal((0.0, 0.0), Statistics.Wilson(0, 0));
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
    public void Fit_AllowsTwoPointsButCannotMeasureTheirQuality()
    {
        var fit = Statistics.Fit([500, 520]);

        Assert.NotNull(fit);
        Assert.Equal(20, fit.Slope);

        // Any line passes exactly through two points: reporting R² = 1 with a ±0
        // band would claim certainty the data cannot support.
        Assert.Null(fit.R2);
        Assert.Null(fit.StdError);
    }

    [Fact]
    public void Fit_TreatsAFlatSeriesAsPerfectlyExplained()
    {
        var fit = Statistics.Fit([500, 500, 500, 500])!;

        Assert.Equal(0, fit.Slope, precision: 6);
        Assert.Equal(1.0, fit.R2!.Value, precision: 6);
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
