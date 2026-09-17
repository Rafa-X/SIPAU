using System.Globalization;
using System.Text;

namespace UPZMG.Web.Services;

/// <summary>
/// Builds RFC-4180 CSV text from any collection. Planeación works in Excel, so
/// every listing in SIPAU can hand its rows straight to a spreadsheet without
/// a server round-trip: the file is generated in the circuit and streamed to
/// the browser by <c>sigieDownload</c> (app.js).
/// </summary>
public static class CsvExport
{
    /// <summary>
    /// Renders <paramref name="rows"/> as CSV using the given column definitions,
    /// e.g. <c>("Área", a =&gt; a.AreaName)</c>.
    /// </summary>
    public static string Build<T>(IEnumerable<T> rows, params (string Header, Func<T, object?> Value)[] columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(',', columns.Select(c => Escape(c.Header))));
        foreach (var row in rows)
            sb.AppendLine(string.Join(',', columns.Select(c => Escape(Format(c.Value(row))))));

        return sb.ToString();
    }

    /// <summary>Timestamped file name, e.g. "usuarios-20260810-1932.csv".</summary>
    public static string FileName(string prefix) =>
        $"{prefix}-{DateTime.Now:yyyyMMdd-HHmm}.csv";

    private static string Format(object? value) => value switch
    {
        null            => string.Empty,
        DateTime date   => date.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        bool flag       => flag ? "Sí" : "No",
        IFormattable num => num.ToString(null, CultureInfo.InvariantCulture),
        _               => value.ToString() ?? string.Empty,
    };

    /// <summary>Quotes a field only when it contains a separator, quote or newline.</summary>
    private static string Escape(string field)
    {
        if (field.IndexOfAny([',', '"', '\n', '\r']) < 0) return field;
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
