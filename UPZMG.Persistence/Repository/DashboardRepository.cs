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

    /// <summary>
    /// Aprovechamiento, deserción and eficiencia terminal, exactly as the university defines them:
    /// the first is the average grade of the period (not the pass rate), the second the share of
    /// definitive withdrawals, and the third is cohort-based, so it needs a generation that has
    /// already had time to finish.
    /// </summary>
    public async Task<AcademicIndicators> GetAcademicIndicatorsAsync()
    {
        var period = await db.Periods.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new { p.Id, Label = p.SchoolCycle + " " + p.SEP_Period })
            .FirstOrDefaultAsync();

        // With no graded period there is nothing to report, and the screen says so.
        period ??= await db.Periods.AsNoTracking()
            .Where(p => db.Kardexes.Any(k => k.PeriodId == p.Id))
            .OrderByDescending(p => p.StartDate)
            .Select(p => new { p.Id, Label = p.SchoolCycle + " " + p.SEP_Period })
            .FirstOrDefaultAsync();

        if (period is null) return AcademicIndicators.None;

        var grades = db.Kardexes.AsNoTracking().Where(k => k.PeriodId == period.Id);
        double? achievement = await grades.AnyAsync()
            ? Math.Round(await grades.AverageAsync(k => k.Grade), 2)
            : null;

        var enrolments = db.CourseEnrollment.AsNoTracking().Where(e => e.PeriodId == period.Id);

        // One row per course, so students are counted once however many subjects they took.
        var enrolled = await enrolments.Select(e => e.StudentId).Distinct().CountAsync();
        var withdrawn = await enrolments
            .Where(e => e.FinalStatus == FinalStatus.BajaDefinitiva)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync();

        double? dropoutRate = enrolled > 0 ? Math.Round(withdrawn * 100.0 / enrolled, 2) : null;

        var byCareer = await grades
            .Join(db.Students.AsNoTracking(), k => k.StudentId, s => s.Id, (k, s) => new { k.Grade, s.CareerId, s.Id })
            .Join(db.Careers.AsNoTracking(), x => x.CareerId, c => c.Id, (x, c) => new { x.Grade, x.Id, Career = c.Name ?? "Sin nombre" })
            .GroupBy(x => x.Career)
            .Select(g => new CareerAchievement(
                g.Key,
                Math.Round(g.Average(x => x.Grade), 2),
                g.Select(x => x.Id).Distinct().Count()))
            .ToListAsync();

        var (efficiency, cohortLabel, graduates, entrants) = await TerminalEfficiencyAsync();

        return new AcademicIndicators(
            period.Label,
            achievement,
            dropoutRate,
            withdrawn,
            enrolled,
            efficiency,
            cohortLabel,
            graduates,
            entrants,
            [.. byCareer.OrderBy(c => c.Career)]);
    }

    /// <summary>
    /// The most recent generation that has actually finished: one whose students are no longer
    /// enrolled. Using the programme's nominal duration instead would report generations that are
    /// still in class as having graduated nobody, which says more about the calendar than about them.
    /// </summary>
    private async Task<(double? Efficiency, string? Label, int Graduates, int Entrants)> TerminalEfficiencyAsync()
    {
        var cohorts = await db.Periods.AsNoTracking()
            .OrderByDescending(p => p.StartDate)
            .Select(p => new
            {
                Label = p.SchoolCycle + " " + p.SEP_Period,
                Entrants = db.Students.Count(s => s.PeriodIdStart == p.Id),
                Graduates = db.Students.Count(s => s.PeriodIdStart == p.Id && s.Status == StudentStatus.Egresado),
                StillEnrolled = db.Students.Count(s => s.PeriodIdStart == p.Id &&
                    (s.Status == StudentStatus.Activo || s.Status == StudentStatus.BajaTemporal)),
            })
            .ToListAsync();

        var finished = cohorts.FirstOrDefault(c => c.Entrants > 0 && c.StillEnrolled == 0);
        if (finished is null) return (null, null, 0, 0);

        return (Math.Round(finished.Graduates * 100.0 / finished.Entrants, 2),
                finished.Label, finished.Graduates, finished.Entrants);
    }
}
