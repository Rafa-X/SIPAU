using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Imports a whole file at once. Every row is checked before anything is written — the rules of the
/// capture forms, keys repeated inside the file, keys already stored, catalogue values — and the
/// rows are then stored in one transaction: all of them, or none when any is refused.
///
/// The screen used to send the rows one by one as it read them. A file with an error halfway left
/// the rows above it stored and the rest not, and re-sending the corrected file duplicated them.
/// </summary>
public class BulkImportRepository(AppDBContext db, StudentsRepository students, EmployeesRepository employees)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public async Task<ImportResult> ImportStudentsAsync(ImportStudents request)
    {
        var rows = request.Rows;
        if (Refusal(rows) is { } refusal) return refusal;

        var problems = rows.Select(r => ImportRules.Check(r.Request, Today)).ToList();
        AddRepeated(rows, problems,
            ImportRules.RepeatedInFile(rows, ("matrícula", s => s.EnrollmentNumber), ("CURP", s => s.CURP)));

        // Against what is stored: one query per key for the whole file, not one per row.
        var enrollments = rows.Select(r => r.Request.EnrollmentNumber?.Trim() ?? "").Where(v => v != "").Distinct().ToList();
        var curps = rows.Select(r => r.Request.CURP?.Trim() ?? "").Where(v => v != "").Distinct().ToList();

        var takenEnrollments = (await db.Students.AsNoTracking()
                .Where(s => enrollments.Contains(s.EnrollmentNumber))
                .Select(s => s.EnrollmentNumber)
                .ToListAsync())
            .Select(ImportRules.Normalize).ToHashSet();
        var takenCurps = (await db.Students.AsNoTracking()
                .Where(s => curps.Contains(s.CURP))
                .Select(s => s.CURP)
                .ToListAsync())
            .Select(ImportRules.Normalize).ToHashSet();

        var careers = (await db.Careers.AsNoTracking().Select(c => c.Id).ToListAsync()).ToHashSet();
        var periods = (await db.Periods.AsNoTracking().Select(p => p.Id).ToListAsync()).ToHashSet();
        var languages = await LanguagesAsync();

        for (int i = 0; i < rows.Count; i++)
        {
            var s = rows[i].Request;

            if (takenEnrollments.Contains(ImportRules.Normalize(s.EnrollmentNumber)))
                problems[i].Add($"Ya existe un alumno con la matrícula '{s.EnrollmentNumber.Trim()}'.");
            if (takenCurps.Contains(ImportRules.Normalize(s.CURP)))
                problems[i].Add($"Ya existe un alumno con la CURP '{s.CURP.Trim()}'.");
            if (s.CareerId != Guid.Empty && !careers.Contains(s.CareerId))
                problems[i].Add("La carrera no existe en el catálogo.");
            if (s.PeriodIdStart != Guid.Empty && !periods.Contains(s.PeriodIdStart))
                problems[i].Add("El periodo de inicio no existe en el catálogo.");
            AddUnknownLanguages(languages, s.IndigenousLanguages, problems[i]);
        }

        var results = rows
            .Select((r, i) => new ImportRowResult(
                r.Line, r.Request.EnrollmentNumber?.Trim() ?? "", FullName(r.Request), problems[i]))
            .ToList();

        return await CommitAsync(request.Commit, results, rows, row => students.CreateStudentAsync(row.Request));
    }

    public async Task<ImportResult> ImportEmployeesAsync(ImportEmployees request)
    {
        var rows = request.Rows;
        if (Refusal(rows) is { } refusal) return refusal;

        var problems = rows.Select(r => ImportRules.Check(r.Request, Today)).ToList();
        AddRepeated(rows, problems, ImportRules.RepeatedInFile(rows,
            ("número de empleado", e => e.EmployeeNumber.ToString()),
            ("dirección de correo", e => e.Email),
            ("RFC", e => e.Rfc),
            ("CURP", e => e.Curp)));

        var numbers = rows.Select(r => r.Request.EmployeeNumber).Distinct().ToList();
        var emails = rows.Select(r => r.Request.Email?.Trim() ?? "").Where(v => v != "").Distinct().ToList();
        var rfcs = rows.Select(r => r.Request.Rfc?.Trim() ?? "").Where(v => v != "").Distinct().ToList();
        var curps = rows.Select(r => r.Request.Curp?.Trim() ?? "").Where(v => v != "").Distinct().ToList();

        var stored = await db.Employees.AsNoTracking()
            .Where(e => numbers.Contains(e.EmployeeNumber) || emails.Contains(e.Email)
                        || rfcs.Contains(e.Rfc) || curps.Contains(e.Curp))
            .Select(e => new { e.EmployeeNumber, e.Email, e.Rfc, e.Curp })
            .ToListAsync();

        var takenNumbers = stored.Select(e => e.EmployeeNumber).ToHashSet();
        var takenEmails = stored.Select(e => ImportRules.Normalize(e.Email)).ToHashSet();
        var takenRfcs = stored.Select(e => ImportRules.Normalize(e.Rfc)).ToHashSet();
        var takenCurps = stored.Select(e => ImportRules.Normalize(e.Curp)).ToHashSet();
        var languages = await LanguagesAsync();

        for (int i = 0; i < rows.Count; i++)
        {
            var e = rows[i].Request;

            if (takenNumbers.Contains(e.EmployeeNumber))
                problems[i].Add($"Ya existe un empleado con el número {e.EmployeeNumber}.");
            if (takenEmails.Contains(ImportRules.Normalize(e.Email)))
                problems[i].Add($"Ya existe un empleado con el correo '{e.Email.Trim()}'.");
            if (takenRfcs.Contains(ImportRules.Normalize(e.Rfc)))
                problems[i].Add($"Ya existe un empleado con el RFC '{e.Rfc.Trim()}'.");
            if (takenCurps.Contains(ImportRules.Normalize(e.Curp)))
                problems[i].Add($"Ya existe un empleado con la CURP '{e.Curp.Trim()}'.");
            AddUnknownLanguages(languages, e.IndigenousLanguages, problems[i]);
        }

        var results = rows
            .Select((r, i) => new ImportRowResult(r.Line, r.Request.EmployeeNumber.ToString(), r.Request.FullName?.Trim() ?? "", problems[i]))
            .ToList();

        return await CommitAsync(request.Commit, results, rows, row => employees.CreateEmployeeAsync(row.Request));
    }

    /// <summary>
    /// Stores the rows when asked to and every one of them passed. One transaction holds them all:
    /// if a row is refused while writing — another capture took the same matrícula after the
    /// check — the rows written before it are rolled back and the batch reports that row.
    /// </summary>
    private async Task<ImportResult> CommitAsync<T>(
        bool commit, List<ImportRowResult> results, IReadOnlyList<ImportRow<T>> rows, Func<ImportRow<T>, Task> create)
    {
        if (!commit || results.Any(r => !r.Accepted)) return new ImportResult(false, results);

        await using var transaction = await db.Database.BeginTransactionAsync();
        for (int i = 0; i < rows.Count; i++)
        {
            try
            {
                await create(rows[i]);
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
            {
                await transaction.RollbackAsync();
                // What the failed save left tracked must not ride along with this context's next save.
                db.ChangeTracker.Clear();

                // A duplicate arrives already worded; anything else the database refused is not
                // something a person can act on from its text.
                var reason = ex is InvalidOperationException ? ex.Message : "La base de datos rechazó esta fila al guardarla.";
                results[i] = results[i] with { Problems = [reason] };
                return new ImportResult(false, results,
                    $"La línea {rows[i].Line} se rechazó al guardar; no se guardó ninguna fila del archivo.");
            }
        }

        await transaction.CommitAsync();
        return new ImportResult(true, results);
    }

    private static ImportResult? Refusal<T>(List<ImportRow<T>> rows) where T : class => rows.Count switch
    {
        0 => new ImportResult(false, [], "El archivo no tiene filas para importar."),
        > ImportRules.MaxRows => new ImportResult(false, [],
            $"El archivo tiene {rows.Count:N0} filas; el máximo por lote es {ImportRules.MaxRows:N0}. Divídelo en varios archivos."),
        _ when rows.Any(row => row?.Request is null) => new ImportResult(false, [], "Hay filas sin datos."),
        _ => null,
    };

    private static void AddRepeated<T>(IReadOnlyList<ImportRow<T>> rows, List<List<string>> problems, Dictionary<int, List<string>> repeated)
    {
        for (int i = 0; i < rows.Count; i++)
            if (repeated.TryGetValue(rows[i].Line, out var reasons)) problems[i].AddRange(reasons);
    }

    private async Task<List<string>> LanguagesAsync() =>
        (await db.IndigenousLanguages.AsNoTracking().Select(l => l.Language).ToListAsync())
            .Where(l => l is not null)
            .Select(l => l!)
            .ToList();

    private static void AddUnknownLanguages(List<string> catalogue, IEnumerable<string>? names, List<string> problems)
    {
        var unknown = LanguageCatalogue.Unknown(catalogue, names);
        if (unknown.Count > 0)
            problems.Add($"No se reconoce la lengua {string.Join(", ", unknown.Select(u => $"«{u}»"))}.");
    }

    private static string FullName(CreateStudent s) =>
        string.Join(' ', new[] { s.Name, s.MiddleName, s.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));
}
