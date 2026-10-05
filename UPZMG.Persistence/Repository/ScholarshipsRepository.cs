using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Scholarships and titulation, one row per student rather than a total per format. Both are
/// reported broken down — by source, by modality — and a total cannot be broken down again.
/// </summary>
public class ScholarshipsRepository(AppDBContext db)
{
    // ── Scholarships ────────────────────────────────────────────────────────

    public async Task<ScolarshipAssigned> CreateScholarshipAsync(CreateScholarship request)
    {
        var enrollment = request.StudentEnrollmentNumber.Trim();
        var studentId = await db.Students.AsNoTracking()
            .Where(s => s.EnrollmentNumber == enrollment)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No hay ningún alumno con la matrícula '{enrollment}'.");

        if (!await db.ScolarshipPrograms.AnyAsync(p => p.Id == request.ScolarshipProgramId))
            throw new InvalidOperationException("El programa de beca indicado no existe.");

        if (!await db.Periods.AnyAsync(p => p.Id == request.PeriodId))
            throw new InvalidOperationException("El periodo indicado no existe.");

        // The unique index would refuse it anyway; saying why is friendlier than a database error.
        if (await db.ScolarshipAssigneds.AnyAsync(s =>
                s.StudentId == studentId && s.PeriodId == request.PeriodId &&
                s.ScolarshipProgramId == request.ScolarshipProgramId))
            throw new InvalidOperationException("Ese alumno ya tiene esa beca en el periodo indicado.");

        var scholarship = new ScolarshipAssigned
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ScolarshipProgramId = request.ScolarshipProgramId,
            PeriodId = request.PeriodId,
            FinantialSource = request.FinantialSource,
            Amount = request.Amount,
            ScolarshipObjective = string.IsNullOrWhiteSpace(request.ScolarshipObjective)
                ? null
                : request.ScolarshipObjective.Trim(),
            AssignedDate = request.AssignedDate,
        };

        db.ScolarshipAssigneds.Add(scholarship);
        await db.SaveGuardingUniquenessAsync("Ese alumno ya tiene esa beca en el periodo indicado.");
        return scholarship;
    }

    public async Task<List<ScholarshipListItem>> GetScholarshipsAsync() =>
        await (
            from s in db.ScolarshipAssigneds.AsNoTracking()
            join student in db.Students.AsNoTracking() on s.StudentId equals student.Id
            join career in db.Careers.AsNoTracking() on student.CareerId equals career.Id
            join program in db.ScolarshipPrograms.AsNoTracking() on s.ScolarshipProgramId equals program.Id
            join period in db.Periods.AsNoTracking() on s.PeriodId equals period.Id
            orderby period.StartDate descending, student.LastName
            select new ScholarshipListItem(
                s.Id,
                student.Name + " " + student.MiddleName + " " + student.LastName,
                student.EnrollmentNumber,
                career.Name ?? "Sin nombre",
                program.OfficialName,
                s.FinantialSource.ToString(),
                s.Amount,
                period.SchoolCycle + " " + period.SEP_Period,
                s.ScolarshipObjective,
                s.AssignedDate))
            .ToListAsync();

    /// <summary>Grants of the active period by source; the chart the students page was missing.</summary>
    public async Task<ScholarshipsBySource> GetScholarshipsBySourceAsync()
    {
        var activePeriod = await db.Periods.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.StartDate)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();

        var query = db.ScolarshipAssigneds.AsNoTracking();

        // With no active period the whole history is better than an empty chart.
        if (activePeriod is { } period && await query.AnyAsync(s => s.PeriodId == period))
            query = query.Where(s => s.PeriodId == period);

        var rows = await query
            .GroupBy(s => s.FinantialSource)
            .Select(g => new { Source = g.Key, Students = g.Select(s => s.StudentId).Distinct().Count(), Amount = g.Sum(s => s.Amount) })
            .ToListAsync();

        var ordered = rows.OrderByDescending(r => r.Students).ToList();

        return new ScholarshipsBySource(
            [.. ordered.Select(r => r.Source.ToString())],
            [.. ordered.Select(r => r.Students)],
            [.. ordered.Select(r => r.Amount)]);
    }

    // ── Titulation ──────────────────────────────────────────────────────────

    public async Task<TitulationProcess> CreateTitulationAsync(CreateTitulation request)
    {
        var enrollment = request.StudentEnrollmentNumber.Trim();
        var student = await db.Students.AsNoTracking()
            .Where(s => s.EnrollmentNumber == enrollment)
            .Select(s => new { s.Id, s.CareerId, s.Status })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No hay ningún alumno con la matrícula '{enrollment}'.");

        // Titling someone who never finished would inflate the rate the SII reads.
        if (student.Status != StudentStatus.Egresado)
            throw new InvalidOperationException("Sólo se titula a un alumno con estatus de egresado.");

        if (await db.TitulationProcesses.AnyAsync(t => t.StudentId == student.Id))
            throw new InvalidOperationException("Ese alumno ya tiene un proceso de titulación registrado.");

        if (!await db.Employees.AnyAsync(e => e.Id == request.TitulationAssessorId))
            throw new InvalidOperationException("El asesor indicado no existe.");

        if (request.TitleExpeditionDate < request.CreditsEgressDate)
            throw new InvalidOperationException("La expedición del título no puede ser anterior al egreso.");

        var titulation = new TitulationProcess
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CareerId = student.CareerId,
            TitulationAssessorId = request.TitulationAssessorId,
            Modality = request.Modality,
            CreditsEgressDate = request.CreditsEgressDate,
            CertificateTitulationDate = request.CertificateTitulationDate,
            TitleExpeditionDate = request.TitleExpeditionDate,
            HasProfessionalLicence = request.HasProfessionalLicence,
            ProfessionalLicenceNumber = string.IsNullOrWhiteSpace(request.ProfessionalLicenceNumber)
                ? null
                : request.ProfessionalLicenceNumber.Trim(),
        };

        db.TitulationProcesses.Add(titulation);
        await db.SaveGuardingUniquenessAsync("Ese alumno ya tiene un proceso de titulación registrado.");
        return titulation;
    }

    public async Task<List<TitulationListItem>> GetTitulationsAsync() =>
        await (
            from t in db.TitulationProcesses.AsNoTracking()
            join student in db.Students.AsNoTracking() on t.StudentId equals student.Id
            join career in db.Careers.AsNoTracking() on t.CareerId equals career.Id
            join assessor in db.Employees.AsNoTracking() on t.TitulationAssessorId equals assessor.Id
            orderby t.TitleExpeditionDate descending
            select new TitulationListItem(
                t.Id,
                student.Name + " " + student.MiddleName + " " + student.LastName,
                student.EnrollmentNumber,
                career.Name ?? "Sin nombre",
                t.Modality.ToString(),
                assessor.FullName,
                t.CreditsEgressDate,
                t.TitleExpeditionDate,
                t.HasProfessionalLicence,
                t.ProfessionalLicenceNumber))
            .ToListAsync();

    /// <summary>Titled students against graduates: the rate the SII asks for, computed.</summary>
    public async Task<TitulationSummary> GetTitulationSummaryAsync()
    {
        var graduates = await db.Students.CountAsync(s => s.Status == StudentStatus.Egresado);
        var titled = await db.TitulationProcesses.CountAsync();

        var byModality = await db.TitulationProcesses.AsNoTracking()
            .GroupBy(t => t.Modality)
            .Select(g => new { Modality = g.Key, Count = g.Count() })
            .ToListAsync();

        return new TitulationSummary(
            graduates,
            titled,
            [.. byModality.OrderByDescending(m => m.Count).Select(m => new TitulationByModality(m.Modality.ToString(), m.Count))]);
    }

    /// <summary>The scholarship programmes an area can choose from.</summary>
    public async Task<List<LookupOption>> GetProgramsAsync() =>
        await db.ScolarshipPrograms.AsNoTracking()
            .OrderBy(p => p.OfficialName)
            .Select(p => new LookupOption(p.Id, p.OfficialName))
            .ToListAsync();
}
