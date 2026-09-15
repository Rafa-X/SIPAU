using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class GeneralDataRepository(AppDBContext db)
{

    public async Task<List<LookupOption>> GetCareersAsync() =>
        await db.Careers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupOption(c.Id, c.Name ?? "Sin nombre"))
            .ToListAsync();

    public async Task<List<LookupOption>> GetAsignaturesAsync(Guid careerId) =>
        await db.Asignatures
            .AsNoTracking()
            .Where(a => a.CareerId == careerId)
            .OrderBy(a => a.AsignatureName)
            .Select(a => new LookupOption(a.Id, a.AsignatureName))
            .ToListAsync();

        public async Task<List<PeriodLookupOption>> GetPeriodsAsync() =>
            await db.Periods
                .AsNoTracking()
                .OrderBy(p => p.StartDate)
                .Select(p => new PeriodLookupOption(
                    p.Id,
                    p.SEP_Period,
                    p.SchoolCycle))
                .ToListAsync();

    public async Task<List<LookupOption>> GetMaxDegreesAsync() =>
        await db.MaxDegrees
            .AsNoTracking()
            .OrderBy(md => md.Name)
            .Select(md => new LookupOption(md.Id, md.Name ?? "Sin nombre"))
            .ToListAsync();

    public async Task<List<LookupOption>> GetContractTypesAsync() =>
        await db.ContractTypes
            .AsNoTracking()
            .OrderBy(ct => ct.Name)
            .Select(ct => new LookupOption(ct.Id, ct.Name ?? "Sin nombre"))
            .ToListAsync();

    public async Task<List<LookupOption>> GetInternalAreasAsync() =>
        await db.InternalAreas
            .AsNoTracking()
            .OrderBy(ia => ia.Name)
            .Select(ia => new LookupOption(ia.Id, ia.Name ?? "Sin nombre"))
            .ToListAsync();

    public async Task<List<LookupOption>> GetTeachersAsync() =>
        await db.Employees
            .AsNoTracking()
            .Where(e => e.IsTeacher)
            .OrderBy(e => e.FullName)
            .Select(e => new LookupOption(e.Id, e.FullName))
            .ToListAsync();

    public async Task<List<LookupOption>> GetFacilitiesAsync() =>
        await db.Set<SchoolFacility>()
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new LookupOption(f.Id, f.Name))
            .ToListAsync();
}