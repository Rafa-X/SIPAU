using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Charts;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class ChartsRepository(AppDBContext db)
{
    
    public async Task<List<ChartCategory>> GetCareersDistributionAsync()
    {
        var result = await (
            from career in db.Careers
            join student in db.Students on career.Id equals student.CareerId
            group student by career into g
            select new ChartCategory(
                g.Key.Alias ?? "Sin nombre",
                g.Count()
            )
        ).ToListAsync();

        return result.OrderByDescending(x => x.Value).ToList();
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

        var result = await (
            from career in db.Careers.AsNoTracking()
            join classGroup in db.ClassGroups.AsNoTracking()
                on career.Id equals classGroup.CareerId
            join registrations in db.Registrations.AsNoTracking()
                on classGroup.Id equals registrations.GroupId
            join students in db.Students.AsNoTracking()
                on registrations.StudentId equals students.Id
            where classGroup.PeriodId == activePeriodId.Value
            group new { classGroup, registrations, students } by career into grouped
            select new GroupDistributionCareer(
                grouped.Key.Alias ?? "Sin nombre",
                grouped.Select(x => x.classGroup.Id).Distinct().Count(),
                grouped.Select(x => x.students.Id).Distinct().Count()
            )
        ).ToListAsync();

        return result
            .OrderByDescending(item => item.Groups)
            .ThenBy(item => item.Career)
            .ToList();
    }
}