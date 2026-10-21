namespace UPZMG.Implementations.Intelligence;

/// <summary>Result of fitting ŷ = a + b·x by ordinary least squares.</summary>
/// <param name="Slope">b: units of y per period.</param>
/// <param name="Intercept">a: fitted value at x = 0.</param>
/// <param name="R2">Goodness of fit (0..1); null with two points, which fit any line exactly.</param>
/// <param name="StdError">Residual standard error s; null with two points.</param>
/// <param name="Count">Observations the line was fitted to.</param>
/// <param name="MeanX">Mean of their positions, x̄.</param>
/// <param name="SumSquaresX">Spread of the positions, Sxx = Σ(x − x̄)².</param>
public sealed record LinearFit(
    double Slope, double Intercept, double? R2, double? StdError,
    int Count = 0, double MeanX = 0, double SumSquaresX = 0)
{
    public double PredictAt(double x) => Intercept + Slope * x;

    /// <summary>
    /// Half-width of the 95 % prediction interval for one new observation at x:
    ///
    ///   t(0.975, n − 2) · s · √(1 + 1/n + (x − x̄)² / Sxx)
    ///
    /// The 1 is the scatter of a single period around the line; the other two terms are the
    /// uncertainty of the line itself, which grows the further x lies from the fitted history.
    /// Null when the error cannot be measured (two points).
    /// </summary>
    public double? PredictionMargin(double x)
    {
        if (StdError is not { } s || Count < 3 || SumSquaresX <= 0) return null;

        var leverage = 1 + 1.0 / Count + (x - MeanX) * (x - MeanX) / SumSquaresX;
        return Statistics.StudentT975(Count - 2) * s * Math.Sqrt(leverage);
    }
}

/// <summary>Pure statistics over plain numbers: no database, no domain types.</summary>
public static class Statistics
{
    /// <summary>
    /// Least-squares line over y indexed at x = 0..n-1. Two points provide the
    /// minimum trend required for an initial enrolment projection, but they fit
    /// any line exactly, so their quality is reported as unknown rather than perfect.
    /// </summary>
    public static LinearFit? Fit(IReadOnlyList<double> y) =>
        Fit([.. Enumerable.Range(0, y.Count).Select(i => (double)i)], y);

    /// <summary>
    /// The same fit over explicit x positions. The positions matter as soon as a point is
    /// missing from the series: treating what is left as consecutive closes the gap up, and
    /// the slope then means "per observation" instead of "per period".
    /// </summary>
    public static LinearFit? Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = y.Count;
        if (n < 2 || x.Count != n) return null;

        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXy += x[i] * y[i];
            sumXx += x[i] * x[i];
        }

        double denominator = n * sumXx - sumX * sumX;
        if (Math.Abs(denominator) < 1e-9) return null;

        double slope = (n * sumXy - sumX * sumY) / denominator;
        double intercept = (sumY - slope * sumX) / n;

        double meanY = sumY / n;
        double ssTot = 0, ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double residual = y[i] - (intercept + slope * x[i]);
            ssRes += residual * residual;
            ssTot += (y[i] - meanY) * (y[i] - meanY);
        }

        // Two observations leave no residual degrees of freedom: the line through them is
        // exact by construction, so neither its fit nor its error can be measured. Saying
        // "R² = 1 with a ±0 band" would claim certainty the data does not support.
        double meanX = sumX / n;
        double sxx = sumXx - n * meanX * meanX;

        if (n == 2) return new LinearFit(slope, intercept, R2: null, StdError: null, n, meanX, sxx);

        // A flat series has no variance to explain, and the line reproduces it exactly.
        double r2 = ssTot < 1e-9 ? 1 : 1 - ssRes / ssTot;
        double stdError = Math.Sqrt(ssRes / (n - 2));

        return new LinearFit(slope, intercept, Math.Clamp(r2, 0, 1), stdError, n, meanX, sxx);
    }

    // Two-sided 95 % critical values of Student's t for 1 to 30 degrees of freedom.
    private static readonly double[] T975 =
    [
        12.706, 4.303, 3.182, 2.776, 2.571, 2.447, 2.365, 2.306, 2.262, 2.228,
        2.201, 2.179, 2.160, 2.145, 2.131, 2.120, 2.110, 2.101, 2.093, 2.086,
        2.080, 2.074, 2.069, 2.064, 2.060, 2.056, 2.052, 2.048, 2.045, 2.042,
    ];

    /// <summary>
    /// The 0.975 quantile of Student's t with the given degrees of freedom: the table up to 30,
    /// then the Cornish–Fisher expansion around the normal 1.96, accurate to three decimals there.
    /// </summary>
    public static double StudentT975(int degreesOfFreedom)
    {
        if (degreesOfFreedom < 1) throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom));
        if (degreesOfFreedom <= T975.Length) return T975[degreesOfFreedom - 1];

        const double z = 1.959964;
        double df = degreesOfFreedom;
        return z + (z * z * z + z) / (4 * df) + (5 * Math.Pow(z, 5) + 16 * z * z * z + 3 * z) / (96 * df * df);
    }

    /// <summary>
    /// The 95 % Wilson score interval of a proportion k / n. Unlike p ± 1.96·√(p(1−p)/n) it stays
    /// inside 0..1 and does not collapse to a point when k is 0 or n, which matters for the small,
    /// lopsided leaves a tree produces. Both bounds are 0 when there is nothing to measure.
    /// </summary>
    public static (double Lower, double Upper) Wilson(int successes, int total)
    {
        if (total <= 0) return (0, 0);

        const double z = 1.959964;
        double p = (double)successes / total;
        double z2n = z * z / total;
        double centre = (p + z2n / 2) / (1 + z2n);
        double half = z * Math.Sqrt(p * (1 - p) / total + z2n / (4 * total)) / (1 + z2n);

        return (Math.Max(0, centre - half), Math.Min(1, centre + half));
    }

    /// <summary>Distance of each point from the mean, in standard deviations. A constant series is all zeros.</summary>
    public static double[] ZScores(IReadOnlyList<double> values)
    {
        int n = values.Count;
        if (n == 0) return [];

        double mean = values.Average();
        double sd = StandardDeviation(values);

        if (sd < 1e-9) return new double[n];
        return [.. values.Select(v => (v - mean) / sd)];
    }

    /// <summary>Population standard deviation.</summary>
    public static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;

        double mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Count);
    }

    /// <summary>Percentage change between consecutive points; the first entry is 0.</summary>
    public static double[] PercentChange(IReadOnlyList<double> values)
    {
        var result = new double[values.Count];
        for (int i = 1; i < values.Count; i++)
        {
            var previous = values[i - 1];
            result[i] = Math.Abs(previous) < 1e-9 ? 0 : (values[i] - previous) / previous * 100;
        }
        return result;
    }
}
