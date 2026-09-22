using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class EmployeesRepository(AppDBContext db)
{
    
    // Teaching staff as captured, newest hires first
    public async Task<List<TeacherListItem>> GetTeachersListAsync() =>
        await db.Employees
            .AsNoTracking()
            .Where(employee => employee.IsTeacher)
            .OrderByDescending(employee => employee.FirstHiredDate)
            .Select(employee => new TeacherListItem(
                employee.Id,
                employee.EmployeeNumber,
                employee.FullName,
                employee.Email,
                employee.HighestEducationLevel.ToString(),
                employee.FirstHiredDate,
                employee.HasDisability))
            .ToListAsync();

    public async Task<Employee> CreateEmployeeAsync(CreateEmployee request)
    {
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

        var matchedLanguageIds = await db.IndigenousLanguages
            .Where(l => l.Language != null && request.IndigenousLanguages.Contains(l.Language))
            .Select(l => l.Id).Distinct().ToListAsync();

        var employee = new Employee
        {
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
        await db.SaveChangesAsync();

        return employee;
    }
}