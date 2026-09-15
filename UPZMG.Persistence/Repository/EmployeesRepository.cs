using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class EmployeesRepository(AppDBContext db)
{
    
    public async Task<Employee> CreateEmployeeAsync(CreateEmployee request)
    {
        var exist = await db.Employees.AnyAsync(s => s.EmployeeNumber == request.EmployeeNumber);
        if( exist)
            throw new InvalidOperationException("Employee with the same employee number already exists.");

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