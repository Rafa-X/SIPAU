using System.Globalization;
using System.Text;

namespace UPZMG.Implementations.DataCapture;

/// <summary>One row of a CSV import, already turned into a capture request, and where it came from.</summary>
/// <param name="Line">Line of the file it was read from; the header is line 1.</param>
public sealed class ImportRow<T>
{
    public int Line { get; set; }
    public required T Request { get; set; }
}

/// <summary>
/// The students of one file. With <see cref="Commit"/> false the API only checks them and writes
/// nothing; with true it stores every row in one transaction, or none of them when any is refused.
/// </summary>
public sealed class ImportStudents
{
    public required Guid RequesterKey { get; set; }
    public List<ImportRow<CreateStudent>> Rows { get; set; } = [];
    public bool Commit { get; set; }
}

/// <summary>The employees of one file; checked or stored like <see cref="ImportStudents"/>.</summary>
public sealed class ImportEmployees
{
    public required Guid RequesterKey { get; set; }
    public List<ImportRow<CreateEmployee>> Rows { get; set; } = [];
    public bool Commit { get; set; }
}

/// <summary>What happened to one row: accepted, or every reason it was refused.</summary>
/// <param name="Key">What identifies the record to a person: the matrícula or the employee number.</param>
/// <param name="Name">The name on the row, so the confirmation lists people and not only keys.</param>
public sealed record ImportRowResult(int Line, string Key, string Name, IReadOnlyList<string> Problems)
{
    public bool Accepted => Problems.Count == 0;
}

/// <summary>The verdict on a whole batch.</summary>
/// <param name="Committed">True only when every row was stored. A refused batch stores nothing.</param>
/// <param name="Message">Why the batch as a whole was refused, when it was not a row's fault.</param>
public sealed record ImportResult(bool Committed, IReadOnlyList<ImportRowResult> Rows, string? Message = null)
{
    public int Accepted => Rows.Count(r => r.Accepted);
    public int Refused => Rows.Count - Accepted;
}

/// <summary>
/// The bytes of an uploaded file as text. Excel's plain "CSV" is written in the computer's ANSI code
/// page with nothing that says so, and reading that as UTF-8 stored "Mar�a" without any error.
/// </summary>
public static class CsvText
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static CsvText() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static string Decode(byte[] bytes)
    {
        if (bytes is [0xEF, 0xBB, 0xBF, ..]) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes is [0xFF, 0xFE, ..]) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes is [0xFE, 0xFF, ..]) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    private static readonly string[] DateFormats = ["yyyy-M-d", "d/M/yyyy", "d-M-yyyy"];

    /// <summary>
    /// A date of a file: the template's own form, or the day-first one Excel writes on a Mexican
    /// computer. Read by the invariant culture, 01/02/2005 became January 2 while 13/02/2005 was
    /// refused, so one file came out with some birth dates changed and others rejected. A year of
    /// two digits is refused: nothing in the cell says which century it means.
    /// </summary>
    public static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>A spreadsheet renamed to .csv, or anything else that is not text: text never carries a NUL.</summary>
    public static bool IsBinary(ReadOnlySpan<byte> bytes)
    {
        if (bytes is [0xFF, 0xFE, ..] or [0xFE, 0xFF, ..]) return false;
        return bytes[..Math.Min(bytes.Length, 8192)].IndexOf((byte)0) >= 0;
    }
}

/// <summary>
/// The rules every captured student and employee passes, whether it comes from a form or from a
/// file, plus the one only a file needs: that it does not repeat its own keys. Pure, so they are
/// tested without a database; the checks against stored records live in the repositories.
/// </summary>
public static class ImportRules
{
    // A file larger than this is better split: one transaction holds every row until it ends.
    public const int MaxRows = 2000;

    public static List<string> Check(CreateStudent s, DateOnly today)
    {
        var problems = new List<string>();

        if (Blank(s.EnrollmentNumber)) problems.Add("Falta la matrícula.");
        if (Blank(s.Name)) problems.Add("Falta el nombre.");
        if (Blank(s.LastName)) problems.Add("Faltan los apellidos.");
        if (Blank(s.Address)) problems.Add("Falta el domicilio.");
        if (Blank(s.BirthCountry)) problems.Add("Falta el país de nacimiento.");
        if (Blank(s.BirthState)) problems.Add("Falta el estado de nacimiento.");
        // Measured as it came, spaces included: that is what goes to an 18-character column.
        if (!Blank(s.CURP) && s.CURP.Length != 18) problems.Add("La CURP debe tener 18 caracteres.");
        if (s.CareerId == Guid.Empty) problems.Add("Falta la carrera.");
        if (s.PeriodIdStart == Guid.Empty) problems.Add("Falta el periodo de inicio.");
        if (s.NumberOfChildren < 0) problems.Add("El número de hijos no puede ser negativo.");
        CheckBirthDate(s.BirthDate, today, problems);

        // The sizes of the columns the values are stored in: longer text reached the database and
        // came back as a server error instead of as a reason.
        CheckLength(s.EnrollmentNumber, 20, "Matrícula", problems);
        CheckLength(s.Name, 100, "Nombre", problems);
        CheckLength(s.MiddleName, 100, "Segundo nombre", problems);
        CheckLength(s.LastName, 100, "Apellidos", problems);
        CheckLength(s.Address, 100, "Domicilio", problems);
        CheckLength(s.PostalCode, 10, "Código postal", problems);
        CheckLength(s.BirthCountry, 100, "País de nacimiento", problems);
        CheckLength(s.BirthState, 100, "Estado de nacimiento", problems);

        // A number outside the list was stored as it came and read back as a value nobody can name.
        CheckDefined(s.Gender, "El sexo", problems);
        CheckDefined(s.EnglishLevel, "El nivel de inglés", problems);
        CheckDefined(s.MaritalStatus, "El estado civil", problems);
        CheckDefined(s.InscriptionType, "El tipo de inscripción", problems);
        CheckDefined(s.Status, "El estatus del alumno", problems);

        return problems;
    }

    public static List<string> Check(CreateEmployee e, DateOnly today)
    {
        var problems = new List<string>();

        if (e.EmployeeNumber <= 0) problems.Add("El número de empleado debe ser mayor que cero.");
        if (Blank(e.FullName)) problems.Add("Falta el nombre completo.");
        if (Blank(e.Email) || !e.Email.Contains('@')) problems.Add("El correo electrónico no es válido.");
        if (Blank(e.Rfc) || e.Rfc.Length is not (12 or 13)) problems.Add("El RFC debe tener 12 o 13 caracteres.");
        if (Blank(e.Curp) || e.Curp.Length != 18) problems.Add("La CURP debe tener 18 caracteres.");
        if (e.ExternalXPYears < 0) problems.Add("Los años de experiencia no pueden ser negativos.");
        CheckBirthDate(e.BirthDate, today, problems);

        if (e.FirstHiredDate == default) problems.Add("Falta la fecha de primera contratación.");
        else if (e.FirstHiredDate > today) problems.Add("La fecha de primera contratación está en el futuro.");
        else if (e.BirthDate != default && e.FirstHiredDate <= e.BirthDate)
            problems.Add("La fecha de primera contratación es anterior al nacimiento.");

        CheckLength(e.FullName, 200, "Nombre completo", problems);
        CheckLength(e.Email, 100, "Correo electrónico", problems);

        CheckDefined(e.Gender, "El sexo", problems);
        CheckDefined(e.HighestEducationLevel, "El nivel máximo de estudios", problems);
        CheckDefined(e.TitleStatus, "El estatus de titulación", problems);

        return problems;
    }

    /// <summary>
    /// The rows that repeat a key of an earlier row of the same file, with the reason. The first
    /// occurrence is kept: it is the one a person reading the file from the top meets first. Blank
    /// keys repeat nothing — a missing CURP is allowed for many students.
    /// </summary>
    public static Dictionary<int, List<string>> RepeatedInFile<T>(
        IReadOnlyList<ImportRow<T>> rows, params (string Label, Func<T, string?> Key)[] keys)
    {
        var repeated = new Dictionary<int, List<string>>();

        foreach (var (label, key) in keys)
        {
            var firstLine = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var value = Normalize(key(row.Request));
                if (value.Length == 0) continue;

                if (firstLine.TryGetValue(value, out var first))
                {
                    if (!repeated.TryGetValue(row.Line, out var reasons)) repeated[row.Line] = reasons = [];
                    reasons.Add($"{label} '{key(row.Request)!.Trim()}' ya aparece en la línea {first} del archivo.");
                }
                else
                {
                    firstLine[value] = row.Line;
                }
            }
        }

        return repeated;
    }

    /// <summary>How keys are compared: trimmed, case and accents aside.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        return new string([.. decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)])
            .ToUpperInvariant();
    }

    private static void CheckBirthDate(DateOnly birthDate, DateOnly today, List<string> problems)
    {
        if (birthDate == default) problems.Add("Falta la fecha de nacimiento.");
        else if (birthDate > today) problems.Add("La fecha de nacimiento está en el futuro.");
        else if (birthDate.Year < 1900) problems.Add("La fecha de nacimiento es anterior a 1900.");
    }

    private static void CheckLength(string? value, int max, string label, List<string> problems)
    {
        if (value is not null && value.Length > max)
            problems.Add($"{label}: máximo {max} caracteres (tiene {value.Length}).");
    }

    private static void CheckDefined<TEnum>(TEnum value, string label, List<string> problems) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value)) problems.Add($"{label} no es un valor del catálogo.");
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);
}
