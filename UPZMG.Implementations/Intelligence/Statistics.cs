namespace UPZMG.Implementations.Intelligence;

/// <summary>Result of fitting ŷ = a + b·x by ordinary least squares.</summary>
/// <param name="Slope">b: units of y per period.</param>
/// <param name="Intercept">a: fitted value at x = 0.</param>
/// <param name="R2">Goodness of fit (0..1); null with two points, which fit any line exactly.</param>
/// <param name="StdError">Residual standard error that sizes the uncertainty band; null with two points.</param>
public sealed record LinearFit(double Slope, double Intercept, double? R2, double? StdError)
{
    public double PredictAt(double x) => Intercept + Slope * x;
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
        if (n == 2) return new LinearFit(slope, intercept, R2: null, StdError: null);

        // A flat series has no variance to explain, and the line reproduces it exactly.
        double r2 = ssTot < 1e-9 ? 1 : 1 - ssRes / ssTot;
        double stdError = Math.Sqrt(ssRes / (n - 2));

        return new LinearFit(slope, intercept, Math.Clamp(r2, 0, 1), stdError);
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
