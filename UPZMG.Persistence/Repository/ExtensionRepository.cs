using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Linkage projects, stored one per project so the counts the 911 and SII formats ask for
/// (by sector, by activity, by period) can be derived instead of transcribed.
/// </summary>
public class ExtensionRepository(AppDBContext db)
{
    public async Task<OutreachProject> CreateAsync(CreateOutreachProject request, Guid createdBy)
    {
        if (!await db.Periods.AnyAsync(p => p.Id == request.PeriodId))
            throw new InvalidOperationException("El periodo indicado no existe.");

        if (request.StartDate is { } start && request.EndDate is { } end && end < start)
            throw new InvalidOperationException("La fecha de término no puede ser anterior a la de inicio.");

        // The enrolment number is optional, but a wrong one would silently lose the link.
        Guid? studentId = null;
        var enrollment = request.StudentEnrollmentNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(enrollment))
        {
            studentId = await db.Students.AsNoTracking()
                .Where(s => s.EnrollmentNumber == enrollment)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();

            if (studentId is null)
                throw new InvalidOperationException($"No hay ningún alumno con la matrícula '{enrollment}'.");
        }

        var project = new OutreachProject
        {
            Id = Guid.NewGuid(),
            PeriodId = request.PeriodId,
            OrganizationName = request.OrganizationName.Trim(),
            Sector = request.Sector.Trim(),
            CompanySize = request.CompanySize.Trim(),
            ActivityType = request.ActivityType.Trim(),
            ProjectName = request.ProjectName.Trim(),
            StudentId = studentId,
            StudentEnrollmentNumber = string.IsNullOrWhiteSpace(enrollment) ? null : enrollment,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AccreditedHours = request.AccreditedHours,
            CreatedBy = createdBy,
            DateCreated = DateTime.UtcNow,
        };

        db.OutreachProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    public async Task<List<OutreachListItem>> GetListAsync()
    {
        var projects = await db.OutreachProjects.AsNoTracking()
            .OrderByDescending(p => p.DateCreated)
            .ToListAsync();

        if (projects.Count == 0) return [];

        var periods = await db.Periods.AsNoTracking()
            .Select(p => new { p.Id, p.IdCode })
            .ToDictionaryAsync(p => p.Id, p => p.IdCode);

        var studentIds = projects.Where(p => p.StudentId is not null).Select(p => p.StudentId!.Value).Distinct().ToList();
        var students = await db.Students.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.MiddleName, s.LastName })
            .ToDictionaryAsync(s => s.Id, s => $"{s.Name} {s.MiddleName} {s.LastName}".Trim());

        return [.. projects.Select(p => new OutreachListItem(
            p.Id,
            p.OrganizationName,
            p.Sector,
            p.CompanySize,
            p.ActivityType,
            p.ProjectName,
            periods.GetValueOrDefault(p.PeriodId, "—"),
            p.StudentId is { } id ? students.GetValueOrDefault(id) : null,
            p.StudentEnrollmentNumber,
            p.StartDate,
            p.EndDate,
            p.AccreditedHours))];
    }
}
