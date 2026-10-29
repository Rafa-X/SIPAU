using Microsoft.EntityFrameworkCore;
using UPZMG.Shared.Enums;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// School spaces: the area's own storage, and the same rows the class groups are scheduled into.
/// Keeping one list means a space cannot exist for the timetable and not for the 911 format.
/// </summary>
public class InfraestructureRepository(AppDBContext db)
{
    private const int MaxProblems = 30;
    private const int MaxProblemLength = 200;

    public async Task<SchoolFacility> CreateAsync(CreateFacility request)
    {
        var name = request.Name.Trim();

        // Two spaces with the same name make the timetable ambiguous for whoever schedules a group.
        if (await db.SchoolFacilities.AnyAsync(f => f.Name == name))
            throw new InvalidOperationException($"Ya existe un espacio registrado con el nombre '{name}'.");

        if (!Enum.IsDefined(request.Type))
            throw new InvalidOperationException("El tipo de espacio no es un valor del catálogo.");

        // Read back on the consult and in the board report, one line each.
        var problems = request.Problems.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
        if (problems.Count > MaxProblems)
            throw new InvalidOperationException($"Registra hasta {MaxProblems} problemas por espacio.");
        if (problems.Any(p => p.Length > MaxProblemLength))
            throw new InvalidOperationException($"Cada problema detectado admite hasta {MaxProblemLength} caracteres.");

        var facility = new SchoolFacility
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = request.Type,
            SquareMeters = request.SquareMeters,
            Capacity = request.Capacity,
            ComputersInOperation = request.ComputersInOperation,
            InUse = request.InUse,
            HasInternet = request.HasInternet,
            HasComputers = request.HasComputers || request.ComputersInOperation > 0,
            SupportDisabilities = request.SupportDisabilities,
            Problems = problems,
        };

        db.SchoolFacilities.Add(facility);
        await db.SaveGuardingUniquenessAsync("Ya existe un espacio registrado con ese nombre.");
        return facility;
    }

    /// <summary>Every registered space, with how many groups of the active period use it.</summary>
    public async Task<List<FacilityListItem>> GetListAsync()
    {
        var activePeriod = await db.Periods.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.StartDate)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();

        var groups = activePeriod is null
            ? []
            : await db.ClassGroups.AsNoTracking()
                .Where(g => g.PeriodId == activePeriod)
                .GroupBy(g => g.FacilityId)
                .Select(g => new { FacilityId = g.Key, Count = g.Count() })
                .ToListAsync();

        var facilities = await db.SchoolFacilities.AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync();

        return [.. facilities.Select(f => new FacilityListItem(
            f.Id,
            f.Name,
            f.Type.Label(),
            f.SquareMeters,
            f.Capacity,
            f.ComputersInOperation,
            f.InUse,
            f.HasInternet,
            f.HasComputers,
            f.SupportDisabilities,
            groups.FirstOrDefault(g => g.FacilityId == f.Id)?.Count ?? 0))];
    }
}
