using System.Globalization;

namespace UPZMG.Web.Components.Shared.Charts;

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
/// The only colours charts may use.
///
/// These resolve to CSS variables rather than fixed hex values, so a chart
/// repaints itself correctly when the user switches light/dark.
/// </summary>
public static class ChartPalette
{
    // They point at chart tokens (app.css) rather than at the theme's roles. The roles were
    // collapsed when the palette became institutional blue — primary, secondary and, in the
    // dark theme, tertiary are one colour — so two series drawn with "secondary" and
    // "primary" came out identical and a legend could not tell them apart.

    /// <summary>Observed / primary measure.</summary>
    public const string Teal = "var(--sipau-chart-a)";

    /// <summary>Modelled or projected values: a lighter step of the same ink.</summary>
    public const string Indigo = "var(--sipau-chart-b)";

    /// <summary>Institutional / structural measure, where it is the only series.</summary>
    public const string Navy = "var(--sipau-chart-a)";

    /// <summary>The second category of a pair; reads apart from the blue without relying on hue alone.</summary>
    public const string Ochre = "var(--sipau-chart-c)";

    public const string Clay = "var(--sipau-chart-c)";

    /// <summary>Reference lines that should recede: a trend, a target.</summary>
    public const string Neutral = "var(--mud-palette-text-secondary)";
}

/// <summary>One axis label: where it sits and at which chart widths it is shown.</summary>
/// <param name="Class">Visibility per width step, e.g. "s3 s4 l3"; see .sipau-chart-xaxis in app.css.</param>
/// <param name="Style">Inline position, held inside the plot.</param>
public readonly record struct ChartAxisLabel(string Text, string Class, string Style);

/// <summary>The labels of an axis and the width steps at which it needs a second row.</summary>
public sealed record ChartAxisLayout(IReadOnlyList<ChartAxisLabel> Labels, string RowClass)
{
    public static readonly ChartAxisLayout Empty = new([], string.Empty);
}

/// <summary>
/// Decides which axis labels can be shown without running into each other.
///
/// The charts are plain SVG with HTML labels and no script, so nothing can measure the text at
/// run time. What is known is how wide the plot is guaranteed to be at each container-query
/// step and, closely enough, how wide each label is; from that every label is placed for
/// every step. It used to be decided by the number of labels alone, which piled long period
/// names on top of each other, and bar charts showed every label regardless.
/// </summary>
public static class ChartAxis
{
    // Narrowest plot of each step: the step's container width minus the y-axis column. The
    // first one is a chart in a card on a 320 px phone.
    // Keep in line with the @container rules of .sipau-chart-xaxis in app.css.
    private static readonly double[] PlotWidths = [180, 236, 296, 376, 456, 616, 836, 1136];

    /// <summary>
    /// Width of a label in Archivo Narrow 600 at the axis size, measured in the browser:
    /// capitals 7.2 px on average, digits 5.8, lower case 5.5, spaces and punctuation 3 to 3.6.
    /// Rounded up, because an estimate that falls short is an overlap.
    /// </summary>
    public static double Width(string label)
    {
        double width = 0;
        foreach (char c in label)
            width += char.IsUpper(c) ? 7.6 : char.IsDigit(c) ? 5.9 : char.IsLetter(c) ? 5.8 : 3.6;
        return width;
    }

    /// <param name="labels">The category names, in axis order.</param>
    /// <param name="at">Horizontal position of label <c>i</c> as a fraction of the plot, 0 to 1.</param>
    /// <param name="stagger">
    /// True for categories: when the names do not fit side by side they alternate on two rows
    /// before any of them is dropped, since a bar without a name cannot be read. A time axis
    /// is thinned instead; its reader interpolates.
    /// </param>
    public static ChartAxisLayout Layout(IReadOnlyList<string> labels, Func<int, double> at, bool stagger)
    {
        int n = labels.Count;
        if (n == 0) return ChartAxisLayout.Empty;

        var widths = labels.Select(Width).ToArray();

        // Air between two labels: short ones (ages, years) read fine close together, long
        // ones need more to stay apart as words.
        double gap = Math.Clamp(widths.Max() * 0.15, 6, 10);

        var classes = new List<string>[n];
        for (int i = 0; i < n; i++) classes[i] = [];
        var rows = new List<string>();

        for (int step = 0; step < PlotWidths.Length; step++)
        {
            double plot = PlotWidths[step];

            // Labels are centred on their mark but never leave the plot, so the ones at the
            // ends slide inwards; the same clamp is applied in CSS.
            (double From, double To) Span(int i)
            {
                double half = widths[i] / 2;
                double centre = Math.Clamp(at(i) * plot, half, Math.Max(half, plot - half));
                return (centre - half, centre + half);
            }

            bool Fits(IEnumerable<int> row)
            {
                double edge = double.NegativeInfinity;
                foreach (int i in row)
                {
                    var (from, to) = Span(i);
                    if (from < edge + gap) return false;
                    edge = to;
                }
                return true;
            }

            var all = Enumerable.Range(0, n).ToList();

            if (Fits(all))
            {
                foreach (int i in all) classes[i].Add($"s{step}");
                continue;
            }

            if (stagger && Fits(all.Where(i => i % 2 == 0)) && Fits(all.Where(i => i % 2 == 1)))
            {
                foreach (int i in all)
                {
                    classes[i].Add($"s{step}");
                    if (i % 2 == 1) classes[i].Add($"l{step}");
                }
                rows.Add($"r{step}");
                continue;
            }

            // Too many for the width: one label every so many, counted from the end, because
            // the latest period is the one being read. Showing only the last always fits.
            List<int> kept = [n - 1];
            for (int stride = 2; stride < n; stride++)
            {
                var candidate = all.Where(i => (n - 1 - i) % stride == 0).ToList();
                if (!Fits(candidate)) continue;
                kept = candidate;
                break;
            }

            // The first label says where the series starts, so it takes the place of the
            // earliest one kept when the count does not land on it.
            if (kept[0] != 0)
            {
                List<int> withFirst = [0, .. kept.Count > 1 ? kept.Skip(1) : kept];
                if (Fits(withFirst)) kept = withFirst;
            }

            foreach (int i in kept) classes[i].Add($"s{step}");
        }

        var result = new ChartAxisLabel[n];
        for (int i = 0; i < n; i++)
        {
            string half = ChartScale.N(Math.Ceiling(widths[i] / 2));
            string pct = (at(i) * 100).ToString("0.##", CultureInfo.InvariantCulture);
            result[i] = new ChartAxisLabel(
                labels[i],
                string.Join(' ', classes[i]),
                $"left:clamp({half}px,{pct}%,calc(100% - {half}px))");
        }

        return new ChartAxisLayout(result, string.Join(' ', rows));
    }
}

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

    /// <summary>Distance between two gridlines, always a round number.</summary>
    public double Step { get; }

    /// <param name="values">Every value that must fit in the plot. NaN marks a gap and is ignored.</param>
    /// <param name="baselineAtZero">
    /// True for bar charts (a bar must grow from zero to be honest);
    /// false for trend lines, where zooming to the data range is clearer.
    /// </param>
    /// <param name="fixedMax">Top of the axis when the measure has one, such as 100 for a percentage.</param>
    public ChartScale(
        IEnumerable<double> values,
        double left, double right, double top, double bottom,
        bool baselineAtZero = false,
        double? fixedMax = null)
    {
        Left = left; Right = right; Top = top; Bottom = bottom;

        var list = values.Where(double.IsFinite).ToList();
        double min = list.Count == 0 ? 0 : list.Min();
        double max = list.Count == 0 ? 1 : list.Max();

        // Counts get whole-number gridlines: the same axis used to read "2, 2, 1, 1, 0".
        bool whole = list.All(v => Math.Abs(v - Math.Round(v)) < 1e-9);
        bool nonNegative = min >= 0;

        if (baselineAtZero) min = Math.Min(0, min);

        double span = max - min;
        if (span < 1e-9) span = Math.Max(1, Math.Abs(max) * 0.2);

        // Room around a line so it never touches the frame — but a measure that cannot be
        // negative never gets an axis that is: the padding used to take enrolment to "-8".
        // Bars need none: the tallest one may reach the top gridline.
        double low = baselineAtZero ? min : min - span * 0.08;
        if (nonNegative) low = Math.Max(0, low);
        double high = fixedMax ?? (baselineAtZero ? max : max + span * 0.08);
        if (high - low < 1e-9) high = low + span;

        Step = NiceStep((high - low) / 4, whole);
        Min = Math.Floor(low / Step + 1e-9) * Step;
        Max = fixedMax ?? Math.Ceiling(high / Step - 1e-9) * Step;
        if (Max - Min < 1e-9) Max = Min + Step;
    }

    // The nearest of 1, 2, 2.5, 5 or 10 times a power of ten, so an axis reads 0, 50, 100
    // instead of 0, 36, 72. Never below 1 when the data are whole numbers.
    private static double NiceStep(double raw, bool whole)
    {
        if (raw <= 0 || !double.IsFinite(raw)) return 1;

        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double fraction = raw / magnitude;

        // 2.5 is not a whole number of units, so a small count skips it.
        bool quarters = !(whole && magnitude < 10);

        double nice = fraction < 1.5 ? 1
                    : fraction < 2.25 || (fraction < 3 && !quarters) ? 2
                    : fraction < 3 ? 2.5
                    : fraction < 7 ? 5
                    : 10;

        double step = nice * magnitude;
        return whole ? Math.Max(1, step) : step;
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

    /// <summary>Gridline values on round numbers, top to bottom.</summary>
    public IReadOnlyList<double> Ticks()
    {
        int count = Math.Max(1, (int)Math.Round((Max - Min) / Step));
        var ticks = new double[count + 1];
        for (int i = 0; i <= count; i++)
            ticks[i] = Max - Step * i;
        return ticks;
    }

    /// <summary>
    /// Formats a number for an SVG attribute. **Always** use this: Blazor would
    /// otherwise render doubles with the request culture, and an es-MX decimal
    /// comma produces an invalid path that silently draws nothing.
    /// </summary>
    public static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
