using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Dashboard;
using UPZMG.Implementations.Settings;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

public class DashboardRepository(AppDBContext db)
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var activePeriod = await db.Periods
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PeriodItem(
                p.Id,
                p.IdCode,
                p.SEP_Period,
                p.SchoolCycle,
                p.StartDate,
                p.EndDate,
                p.IsActive))
            .FirstOrDefaultAsync();

        var careers = await db.Careers
            .AsNoTracking()
            .Select(c => new
            {
                Alias = c.Alias ?? "Sin alias",
                Name = c.Name ?? "Sin nombre",
                Students = db.Students.Count(s => s.CareerId == c.Id && s.Status == StudentStatus.Activo),
            })
            .ToListAsync();

        var intake = await db.Periods
            .AsNoTracking()
            .OrderBy(p => p.StartDate)
            .Select(p => new PeriodIntake(
                p.Id,
                p.SchoolCycle + " " + p.SEP_Period,
                db.Students.Count(s => s.PeriodIdStart == p.Id)))
            .ToListAsync();

        var summary = new DashboardSummary
        {
            ActivePeriod = activePeriod,
            TotalStudents = await db.Students.CountAsync(),
            ActiveStudents = await db.Students.CountAsync(s => s.Status == StudentStatus.Activo),
            TotalTeachers = await db.Employees.CountAsync(e => e.IsTeacher),
            // EducationLevel lists every postgraduate degree from Posgrado upward.
            TeachersWithPostgrad = await db.Employees.CountAsync(e =>
                e.IsTeacher && e.HighestEducationLevel >= EducationLevel.Posgrado),
            StudentsByCareer = careers
                .OrderByDescending(c => c.Students)
                .ThenBy(c => c.Name)
                .Select(c => new CareerStudents(c.Alias, c.Name, c.Students))
                .ToList(),
            IntakeByPeriod = intake,
        };

        // Period-scoped figures only exist once a period is active.
        if (activePeriod is null)
            return summary;

        var groups = db.ClassGroups.AsNoTracking().Where(g => g.PeriodId == activePeriod.Id);

        summary.NewEnrollments = await db.Students.CountAsync(s => s.PeriodIdStart == activePeriod.Id);
        summary.ClassGroups = await groups.CountAsync();
        summary.GroupCapacity = await groups.SumAsync(g => (int?)g.Capacity) ?? 0;
        summary.GroupEnrolled = await groups.SumAsync(g => (int?)g.Enrolled) ?? 0;

        return summary;
    }
}
