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

        public async Task<List<LookupOption>> GetPeriodsAsync() =>
            await db.Periods
                .AsNoTracking()
                .OrderByDescending(p => p.StartDate)
                .Select(p => new LookupOption(
                    p.Id,
                    p.IdCode))
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
}