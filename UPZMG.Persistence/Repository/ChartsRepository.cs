using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Charts;
using UPZMG.Persistence.Models;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

public class ChartsRepository(AppDBContext db)
{
    
    /// <summary>
    /// Active students by programme and sex. Every official format asks for the M/H split,
    /// so it is computed from the records rather than typed into each one by hand.
    /// </summary>
    public async Task<GenderDistribution> GetGenderDistributionAsync()
    {
        var rows = await (
            from student in db.Students.AsNoTracking()
            join career in db.Careers.AsNoTracking() on student.CareerId equals career.Id
            where student.Status == StudentStatus.Activo
            group student by new { Career = career.Alias ?? career.Name ?? "Sin nombre", student.Gender } into g
            select new { g.Key.Career, g.Key.Gender, Count = g.Count() }
        ).ToListAsync();

        var careers = rows.Select(r => r.Career).Distinct().OrderBy(c => c).ToList();

        int CountOf(string career, Gender gender) =>
            rows.FirstOrDefault(r => r.Career == career && r.Gender == gender)?.Count ?? 0;

        return new GenderDistribution(
            careers,
            [.. careers.Select(c => CountOf(c, Gender.M))],
            [.. careers.Select(c => CountOf(c, Gender.H))]);
    }

    /// <summary>
    /// Active students per programme. Every other figure on the page counts active students, so
    /// including withdrawals and graduates here made the same programme look larger next to the
    /// chart beside it.
    /// </summary>
    public async Task<List<ChartCategory>> GetCareersDistributionAsync()
    {
        var result = await (
            from career in db.Careers.AsNoTracking()
            join student in db.Students.AsNoTracking() on career.Id equals student.CareerId
            where student.Status == StudentStatus.Activo
            group student by career into g
            select new ChartCategory(
                g.Key.Alias ?? g.Key.Name ?? "Sin nombre",
                g.Count()
            )
        ).ToListAsync();

        return result.OrderByDescending(x => x.Value).ThenBy(x => x.Label).ToList();
    }

    public async Task<List<GroupDistributionCareer>> GetGroupsDistributionByCareerAsync()
    {

        var activePeriodId = await db.Periods
            .AsNoTracking()
            .Where(period => period.IsActive)
            .Select(period => (Guid?)period.Id)
            .FirstOrDefaultAsync();

        if (activePeriodId is null)
            return [];

        // Counted in two queries on purpose. Joining groups to enrolments dropped every group
        // nobody had enrolled in yet, so a programme that had just opened its groups appeared
        // with fewer of them than it has — or disappeared from the chart altogether.
        var groups = await db.ClassGroups
            .AsNoTracking()
            .Where(classGroup => classGroup.PeriodId == activePeriodId.Value)
            .GroupBy(classGroup => classGroup.CareerId)
            .Select(grouped => new { CareerId = grouped.Key, Groups = grouped.Count() })
            .ToListAsync();

        if (groups.Count == 0) return [];

        var enrolled = await (
            from classGroup in db.ClassGroups.AsNoTracking()
            join registration in db.CourseEnrollment.AsNoTracking()
                on classGroup.Id equals registration.GroupId
            where classGroup.PeriodId == activePeriodId.Value
            group registration.StudentId by classGroup.CareerId into grouped
            select new { CareerId = grouped.Key, Students = grouped.Distinct().Count() })
            .ToListAsync();

        var names = await db.Careers
            .AsNoTracking()
            .ToDictionaryAsync(career => career.Id, career => career.Alias ?? career.Name ?? "Sin nombre");

        return groups
            .Select(item => new GroupDistributionCareer(
                names.GetValueOrDefault(item.CareerId, "Sin nombre"),
                item.Groups,
                enrolled.FirstOrDefault(e => e.CareerId == item.CareerId)?.Students ?? 0))
            .OrderByDescending(item => item.Groups)
            .ThenBy(item => item.Career)
            .ToList();
    }

    // Teaching staff by highest degree and by years since they were first hired
    public async Task<TeachersDistribution> GetTeachersDistributionAsync()
    {
        var teachers = await db.Employees
            .AsNoTracking()
            .Where(employee => employee.IsTeacher)
            .Select(employee => new { employee.HighestEducationLevel, employee.FirstHiredDate })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var levels = new (string Label, EducationLevel Level)[]
        {
            ("Licenciatura", EducationLevel.Licenciatura),
            ("Ingeniería", EducationLevel.Ingenieria),
            ("Especialidad", EducationLevel.Especialidad),
            ("Posgrado", EducationLevel.Posgrado),
            ("Maestría", EducationLevel.Maestria),
            ("Doctorado", EducationLevel.Doctorado),
        };

        var byLevel = levels
            .Select(level => new ChartCategory(
                level.Label,
                teachers.Count(teacher => teacher.HighestEducationLevel == level.Level)))
            .Where(category => category.Value > 0)
            .ToList();

        var buckets = new (string Label, int From, int To)[]
        {
            ("0-4 años", 0, 4),
            ("5-9 años", 5, 9),
            ("10-14 años", 10, 14),
            ("15 o más", 15, int.MaxValue),
        };

        var bySeniority = buckets
            .Select(bucket => new ChartCategory(
                bucket.Label,
                teachers.Count(teacher =>
                {
                    var years = today.Year - teacher.FirstHiredDate.Year;
                    if (today < teacher.FirstHiredDate.AddYears(years)) years--;
                    return years >= bucket.From && years <= bucket.To;
                })))
            .ToList();

        return new TeachersDistribution(teachers.Count, byLevel, bySeniority);
    }
}