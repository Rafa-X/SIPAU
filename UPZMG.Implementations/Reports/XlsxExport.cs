using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace UPZMG.Implementations.Reports;

/// <summary>One cell. Numbers are written as numbers so Excel can total them.</summary>
public sealed record XlsxCell(string? Text = null, double? Number = null, bool Bold = false)
{
    public static XlsxCell Label(string text, bool bold = false) => new(Text: text, Bold: bold);

    public static XlsxCell Value(double value) => new(Number: value);

    /// <summary>
    /// A captured answer. Anything that parses as a number is written as one:
    /// a 911 count stored as text is a spreadsheet the área cannot add up.
    /// </summary>
    public static XlsxCell Answer(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new XlsxCell();

        return ReportFormState.TryParseNumber(raw, out var number) ? Value(number) : Label(raw);
    }
}

public sealed record XlsxSheet(string Name, List<List<XlsxCell>> Rows);

/// <summary>
/// Writes a real .xlsx workbook with no external package.
///
/// The alternatives were CSV — one flat sheet, no numeric typing, no tabs — or a
/// licensed spreadsheet component, which is the team's call and not something to
/// slip into a UI commit. An xlsx is a zip with a handful of XML parts, so the
/// honest middle ground is to write those parts.
/// </summary>
public static class XlsxExport
{
    private const string SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelsNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string DocRelsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] Build(IReadOnlyList<XlsxSheet> sheets)
    {
        if (sheets.Count == 0) sheets = [new XlsxSheet("Hoja1", [])];

        var names = UniqueSheetNames(sheets);

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
            Write(zip, "_rels/.rels", RootRels());
            Write(zip, "xl/workbook.xml", Workbook(names));
            Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
            Write(zip, "xl/styles.xml", Styles());

            for (var i = 0; i < sheets.Count; i++)
                Write(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i].Rows));
        }

        return buffer.ToArray();
    }

    /// <summary>A safe, dated file name for a workbook.</summary>
    public static string FileName(string prefix) =>
        $"{XmlConvert.EncodeLocalName(prefix)}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

    // ── Parts ───────────────────────────────────────────────────────────────
    private static string ContentTypes(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        sb.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        sb.Append("""<Default Extension="xml" ContentType="application/xml"/>""");
        sb.Append("""<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        sb.Append("""<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");

        for (var i = 1; i <= sheetCount; i++)
            sb.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");

        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string RootRels() =>
        $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="{RelsNs}"><Relationship Id="rId1" Type="{DocRelsNs}/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private static string Workbook(IReadOnlyList<string> names)
    {
        var sb = new StringBuilder();
        sb.Append($"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="{SpreadsheetNs}" xmlns:r="{DocRelsNs}"><sheets>""");

        for (var i = 0; i < names.Count; i++)
            sb.Append($"""<sheet name="{Escape(names[i])}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");

        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append($"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="{RelsNs}">""");

        for (var i = 1; i <= sheetCount; i++)
            sb.Append($"""<Relationship Id="rId{i}" Type="{DocRelsNs}/worksheet" Target="worksheets/sheet{i}.xml"/>""");

        // Styles comes after the sheets so its id cannot collide with theirs.
        sb.Append($"""<Relationship Id="rId{sheetCount + 1}" Type="{DocRelsNs}/styles" Target="styles.xml"/>""");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    /// <summary>Two formats only: normal (index 0) and bold (index 1).</summary>
    private static string Styles() =>
        $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="{SpreadsheetNs}"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="1"><fill><patternFill patternType="none"/></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>""";

    private static string Sheet(List<List<XlsxCell>> rows)
    {
        var sb = new StringBuilder();
        sb.Append($"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="{SpreadsheetNs}"><cols><col min="1" max="1" width="52" customWidth="1"/><col min="2" max="30" width="18" customWidth="1"/></cols><sheetData>""");

        for (var r = 0; r < rows.Count; r++)
        {
            sb.Append($"""<row r="{r + 1}">""");

            for (var c = 0; c < rows[r].Count; c++)
            {
                var cell = rows[r][c];
                if (cell.Number is null && string.IsNullOrEmpty(cell.Text)) continue;

                var reference = $"{ColumnName(c)}{r + 1}";
                var style = cell.Bold ? " s=\"1\"" : string.Empty;

                if (cell.Number is { } number)
                {
                    sb.Append($"""<c r="{reference}"{style}><v>{number.ToString("R", CultureInfo.InvariantCulture)}</v></c>""");
                }
                else
                {
                    // Inline strings keep one part per sheet; a shared-strings
                    // table would buy nothing at this size.
                    sb.Append($"""<c r="{reference}"{style} t="inlineStr"><is><t xml:space="preserve">{Escape(cell.Text!)}</t></is></c>""");
                }
            }

            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static void Write(ZipArchive zip, string path, string xml)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(xml);
    }

    /// <summary>A1-style column name: 0 → A, 25 → Z, 26 → AA.</summary>
    internal static string ColumnName(int index)
    {
        var name = string.Empty;
        for (var i = index; i >= 0; i = i / 26 - 1)
            name = (char)('A' + i % 26) + name;
        return name;
    }

    /// <summary>
    /// Excel rejects a workbook whose tabs collide or use []:*?/\, and silently
    /// truncates past 31 characters — which would turn two long section titles
    /// into the same name and corrupt the file.
    /// </summary>
    private static List<string> UniqueSheetNames(IReadOnlyList<XlsxSheet> sheets)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new List<string>(sheets.Count);

        foreach (var sheet in sheets)
        {
            // Control characters have to go too: a newline is legal inside a cell
            // and fatal in a tab name, and a title pasted from the printed form
            // can easily carry one.
            var clean = new string([.. (sheet.Name ?? "Hoja")
                .Select(ch => "[]:*?/\\".Contains(ch) || char.IsControl(ch) ? ' ' : ch)]).Trim();

            if (clean.Length == 0) clean = "Hoja";
            if (clean.Length > 31) clean = clean[..31];

            var candidate = clean;
            for (var n = 2; !used.Add(candidate); n++)
            {
                var suffix = $" ({n})";
                candidate = clean.Length + suffix.Length > 31
                    ? clean[..(31 - suffix.Length)] + suffix
                    : clean + suffix;
            }

            names.Add(candidate);
        }

        return names;
    }

    private static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            // Control characters are illegal in XML 1.0 and would make Excel
            // reject the workbook — including the separator used to join
            // multi-select answers.
            if (ch is not ('\t' or '\n' or '\r') && ch < 0x20)
            {
                sb.Append(' ');
                continue;
            }

            sb.Append(ch switch
            {
                '<' => "&lt;",
                '>' => "&gt;",
                '&' => "&amp;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => ch.ToString(),
            });
        }
        return sb.ToString();
    }
}
