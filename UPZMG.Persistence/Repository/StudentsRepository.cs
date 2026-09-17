using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Charts;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class StudentsRepository(AppDBContext db)
{
    private static readonly PasswordHasher<SystemUser> PasswordHasher = new();

    public async Task<Student> CreateStudentAsync(CreateStudent request)
    {
        var exist = await db.Students.AnyAsync(s => s.EnrollmentNumber == request.EnrollmentNumber);
        if( exist)
            throw new InvalidOperationException("Student with the same enrollment number already exists.");

        var matchedLanguageIds = await db.IndigenousLanguages
            .Where(l => l.Language != null && request.IndigenousLanguages.Contains(l.Language))
            .Select(l => l.Id).Distinct().ToListAsync();
        
        var student = new Student
        {
            EnrollmentNumber = request.EnrollmentNumber,
            CURP = request.CURP,
            Name = request.Name,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            PostalCode = request.PostalCode,
            EnglishLevel = request.EnglishLevel,
            MaritalStatus = request.MaritalStatus,
            NumberOfChildren = request.NumberOfChildren,
            Works = request.Works,
            BirthCountry = request.BirthCountry,
            BirthState = request.BirthState,
            Address = request.Address,
            CareerId = request.CareerId,
            PeriodIdStart = request.PeriodIdStart,
            InscriptionType = request.InscriptionType,
            Status = request.Status,
        };

        foreach (var languageId in matchedLanguageIds)
        {
            db.IndigenousLanguagesSpeakers.Add(new IndigenousLanguagesSpeakers
            {
                Id = Guid.NewGuid(),
                SpeakerId = student.Id,
                LanguageId = languageId
            });
        }

        student.IndigenousLanguages = matchedLanguageIds;
        db.Students.Add(student);
        await db.SaveChangesAsync();

        return student;
    }

    public async Task<StudentsListResponse> GetStudentsListAsync(StudentsListRequest request)
    {
        var query =
            from s in db.Students.AsNoTracking()
            join c in db.Careers.AsNoTracking() on s.CareerId equals c.Id
            join p in db.Periods.AsNoTracking() on s.PeriodIdStart equals p.Id
            where 
                (request.CareerId == null || s.CareerId == request.CareerId) &&
                (request.PeriodId == null || s.PeriodIdStart == request.PeriodId)
            select new { Student = s, Career = c, Period = p };

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Student.Name, $"%{term}%") ||
                EF.Functions.Like(x.Student.LastName, $"%{term}%") ||
                EF.Functions.Like(x.Student.MiddleName, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.EnrollmentNumber))
            query = query.Where(x => EF.Functions.Like(x.Student.EnrollmentNumber, $"%{request.EnrollmentNumber.Trim()}%"));

        // Cap page size to keep listing queries cheap regardless of client input.
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var page = request.Page <= 0 ? 1 : request.Page;

        var items = await query
            .OrderBy(x => x.Student.LastName).ThenBy(x => x.Student.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StudentListItem
            {
                Id = x.Student.Id,
                EnrollmentNumber = x.Student.EnrollmentNumber,
                Name = $"{x.Student.Name} {x.Student.MiddleName} {x.Student.LastName}",
                Career = x.Career.Name,
                Period = x.Period.SEP_Period,
                Status = x.Student.Status,
            }).ToListAsync();

        return new StudentsListResponse { Items = items, TotalCount = await query.CountAsync() };
    }

    public Task<List<StudentsAges>> GetStudentsAgesNew() => GetStudentsAgesAsync(newOnly: true);

    public Task<List<StudentsAges>> GetStudentsAgesAll() => GetStudentsAgesAsync(newOnly: false);

    private async Task<List<StudentsAges>> GetStudentsAgesAsync(bool newOnly)
    {
        var students = await db.Students
            .AsNoTracking()
            .Where(s => !newOnly || s.InscriptionType == UPZMG.Shared.Enums.InscriptionType.NuevoIngreso)
            .Select(s => s.BirthDate)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return students
            .Select(birthDate => CalculateAge(birthDate, today))
            .GroupBy(age => age)
            .OrderBy(group => group.Key)
            .Select(group => new StudentsAges
            {
                Name = group.Key.ToString(),
                Values = group.Select(age => (double)age).ToList(),
                Color = "var(--mud-palette-secondary)"
            })
            .ToList();
    }

    private static int CalculateAge(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }
}