using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

public class EmployeesRepository(AppDBContext db)
{
    
    // The staff as captured, newest hires first. Everybody: the form registers people who do not
    // teach too, and with only teachers listed nobody could find them again or correct them.
    public async Task<List<EmployeeListItem>> GetEmployeesListAsync() =>
        (await db.Employees
            .AsNoTracking()
            .OrderByDescending(employee => employee.FirstHiredDate)
            .Select(employee => new
            {
                employee.Id, employee.EmployeeNumber, employee.FullName, employee.Email,
                employee.HighestEducationLevel, employee.FirstHiredDate, employee.HasDisability, employee.IsTeacher,
            })
            .ToListAsync())
            // The label is built in memory: it is wording, not something to ask SQL for.
            .Select(employee => new EmployeeListItem(
                employee.Id,
                employee.EmployeeNumber,
                employee.FullName,
                employee.Email,
                employee.HighestEducationLevel.Label(),
                employee.FirstHiredDate,
                employee.HasDisability,
                employee.IsTeacher))
            .ToList();

    /// <summary>The whole employee, to fill the correction form.</summary>
    public async Task<UpdateEmployee?> GetEmployeeAsync(Guid id)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) return null;

        var languages = await db.IndigenousLanguagesSpeakers.AsNoTracking()
            .Where(s => s.SpeakerId == id)
            .Join(db.IndigenousLanguages.AsNoTracking(), s => s.LanguageId, l => l.Id, (s, l) => l.Language)
            .Where(language => language != null)
            .ToListAsync();

        return new UpdateEmployee
        {
            Id = employee.Id,
            RequesterKey = Guid.Empty,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.FullName,
            Email = employee.Email,
            Rfc = employee.Rfc,
            Curp = employee.Curp,
            Gender = employee.Gender,
            BirthDate = employee.BirthDate,
            HighestEducationLevel = employee.HighestEducationLevel,
            TitleStatus = employee.TitleStatus,
            IndigenousLanguages = [.. languages!],
            HasDisability = employee.HasDisability,
            ExternalXPYears = employee.ExternalXPYears,
            FirstHiredDate = employee.FirstHiredDate,
            IsTeacher = employee.IsTeacher,
        };
    }

    /// <summary>
    /// Corrects a captured employee. Employee number, e-mail, RFC and CURP stay unique among
    /// the others: the row being edited is excluded so saving it unchanged is not a conflict.
    /// </summary>
    public async Task<Employee> UpdateEmployeeAsync(UpdateEmployee request)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == request.Id)
            ?? throw new InvalidOperationException("El empleado ya no existe.");

        RequireCaptureRules(request);

        if (await db.Employees.AnyAsync(e => e.Id != request.Id && e.EmployeeNumber == request.EmployeeNumber))
            throw new InvalidOperationException($"Ya existe otro empleado con el número '{request.EmployeeNumber}'.");

        if (await db.Employees.AnyAsync(e => e.Id != request.Id && e.Email == request.Email))
            throw new InvalidOperationException($"Ya existe otro empleado con el correo '{request.Email}'.");

        if (await db.Employees.AnyAsync(e => e.Id != request.Id && e.Rfc == request.Rfc))
            throw new InvalidOperationException($"Ya existe otro empleado con el RFC '{request.Rfc}'.");

        if (await db.Employees.AnyAsync(e => e.Id != request.Id && e.Curp == request.Curp))
            throw new InvalidOperationException($"Ya existe otro empleado con la CURP '{request.Curp}'.");

        employee.EmployeeNumber = request.EmployeeNumber;
        employee.FullName = request.FullName;
        employee.Email = request.Email;
        employee.Rfc = request.Rfc;
        employee.Curp = request.Curp;
        employee.Gender = request.Gender;
        employee.BirthDate = request.BirthDate;
        employee.HighestEducationLevel = request.HighestEducationLevel;
        employee.TitleStatus = request.TitleStatus;
        employee.HasDisability = request.HasDisability;
        employee.ExternalXPYears = request.ExternalXPYears;
        employee.FirstHiredDate = request.FirstHiredDate;
        employee.IsTeacher = request.IsTeacher;

        var previous = db.IndigenousLanguagesSpeakers.Where(s => s.SpeakerId == request.Id);
        db.IndigenousLanguagesSpeakers.RemoveRange(previous);

        var languageIds = await LanguageCatalogue.ResolveAsync(db, request.IndigenousLanguages);

        foreach (var languageId in languageIds)
        {
            db.IndigenousLanguagesSpeakers.Add(new IndigenousLanguagesSpeakers
            {
                Id = Guid.NewGuid(),
                SpeakerId = employee.Id,
                LanguageId = languageId,
            });
        }

        await db.SaveGuardingUniquenessAsync("Ya existe otro empleado con ese número, correo, RFC o CURP.");
        return employee;
    }

    /// <summary>
    /// The capture rules, here as well as in the form: an employee number of zero, a blank name or
    /// an address without "@" sent to the API was stored, and text longer than its column came
    /// back as a server error.
    /// </summary>
    private static void RequireCaptureRules(CreateEmployee request)
    {
        var problems = ImportRules.Check(request, DateOnly.FromDateTime(DateTime.Today));
        if (problems.Count > 0) throw new InvalidOperationException(string.Join(" ", problems));
    }

    public async Task<Employee> CreateEmployeeAsync(CreateEmployee request)
    {
        RequireCaptureRules(request);

        var existEmployeeNumber = await db.Employees.AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber);
        if (existEmployeeNumber)
            throw new InvalidOperationException($"Ya existe un empleado con el número de empleado '{request.EmployeeNumber}'.");

        var existEmail = await db.Employees.AnyAsync(e => e.Email == request.Email);
        if (existEmail)
            throw new InvalidOperationException($"Ya existe un empleado con el correo '{request.Email}'.");

        var existRfc = await db.Employees.AnyAsync(e => e.Rfc == request.Rfc);
        if (existRfc)
            throw new InvalidOperationException($"Ya existe un empleado con el RFC '{request.Rfc}'.");

        var existCurp = await db.Employees.AnyAsync(e => e.Curp == request.Curp);
        if (existCurp)
            throw new InvalidOperationException($"Ya existe un empleado con la CURP '{request.Curp}'.");

        var matchedLanguageIds = await LanguageCatalogue.ResolveAsync(db, request.IndigenousLanguages);

        var employee = new Employee
        {
            // Assigned here rather than by the database: the language links below copy this id,
            // and before the entity is saved any other source for it would be an empty Guid.
            Id = Guid.NewGuid(),
            EmployeeNumber = request.EmployeeNumber,
            FullName = request.FullName,
            Email = request.Email,
            Rfc = request.Rfc,
            Curp = request.Curp,
            Gender = request.Gender,
            BirthDate = request.BirthDate,
            HighestEducationLevel = request.HighestEducationLevel,
            TitleStatus = request.TitleStatus,
            HasDisability = request.HasDisability,
            ExternalXPYears = request.ExternalXPYears,
            FirstHiredDate = request.FirstHiredDate,
            IsTeacher = request.IsTeacher,
        };

        foreach (var languageId in matchedLanguageIds)
        {
            db.IndigenousLanguagesSpeakers.Add(new IndigenousLanguagesSpeakers
            {
                Id = Guid.NewGuid(),
                SpeakerId = employee.Id,
                LanguageId = languageId
            });
        }

        employee.IndigenousLanguages = matchedLanguageIds;
        db.Employees.Add(employee);
        await db.SaveGuardingUniquenessAsync("Ya existe otro empleado con ese número, correo, RFC o CURP.");

        return employee;
    }
}