using System.Globalization;

namespace UPZMG.Web.Components.Shared.Charts;

/// <summary>
/// One plotted series. Immutable on purpose: a chart never mutates the data it
/// is given, it only projects it.
/// </summary>
/// <param name="Name">Legend label.</param>
/// <param name="Values">Values, one per category. Must match the label count.</param>
/// <param name="Color">Any CSS colour.</param>
/// <param name="Dashed">Render the stroke dashed (used for modelled/trend series).</param>
/// <param name="Filled">Fill the area beneath the line with a gradient.</param>
public sealed record ChartSeriesData(
    string Name,
    IReadOnlyList<double> Values,
    string Color,
    bool Dashed = false,
    bool Filled = false);

/// <summary>
/// Maps data values to SVG coordinates inside a plot rectangle.
///
/// Single responsibility: this type knows nothing about drawing — it only does
/// the arithmetic. Both <c>TrendChart</c> and <c>BarChart</c> depend on this
/// abstraction rather than duplicating their own projection maths.
/// </summary>
public sealed class ChartScale
{
    public double Min { get; }
    public double Max { get; }
    public double Left { get; }
    public double Right { get; }
    public double Top { get; }
    public double Bottom { get; }

    /// <param name="values">Every value that must fit in the plot.</param>
    /// <param name="baselineAtZero">
    /// True for bar charts (a bar must grow from zero to be honest);
    /// false for trend lines, where zooming to the data range is clearer.
    /// </param>
    public ChartScale(
        IEnumerable<double> values,
        double left, double right, double top, double bottom,
        bool baselineAtZero = false)
    {
        Left = left; Right = right; Top = top; Bottom = bottom;

        var list = values as IReadOnlyList<double> ?? values.ToList();
        double min = list.Count == 0 ? 0 : list.Min();
        double max = list.Count == 0 ? 1 : list.Max();

        if (baselineAtZero) min = Math.Min(0, min);

        // Headroom so the plot never collides with the frame.
        double span = max - min;
        double pad = span < 1e-9 ? Math.Max(1, Math.Abs(max) * 0.1) : span * 0.12;

        Min = baselineAtZero ? min : min - pad;
        Max = max + pad;
        if (Max - Min < 1e-9) Max = Min + 1;
    }

    public double Height => Bottom - Top;
    public double Width => Right - Left;

    /// <summary>Value → vertical pixel.</summary>
    public double Y(double value) => Bottom - (value - Min) / (Max - Min) * Height;

    /// <summary>Category index → horizontal pixel, points ON the axis edges (line charts).</summary>
    public double X(int index, int count) =>
        count <= 1 ? (Left + Right) / 2 : Left + index * Width / (count - 1);

    /// <summary>Category index → centre of its band (bar charts).</summary>
    public double BandCenter(int index, int count) =>
        count <= 0 ? (Left + Right) / 2 : Left + (index + 0.5) * Width / count;

    /// <summary>Width of one category band.</summary>
    public double BandWidth(int count) => count <= 0 ? Width : Width / count;

    /// <summary>Evenly spaced gridline values, top to bottom.</summary>
    public IReadOnlyList<double> Ticks(int count = 4)
    {
        var ticks = new double[count + 1];
        for (int i = 0; i <= count; i++)
            ticks[i] = Max - (Max - Min) * i / count;
        return ticks;
    }

    /// <summary>
    /// Formats a number for an SVG attribute. **Always** use this: Blazor would
    /// otherwise render doubles with the request culture, and an es-MX decimal
    /// comma produces an invalid path that silently draws nothing.
    /// </summary>
    public static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
