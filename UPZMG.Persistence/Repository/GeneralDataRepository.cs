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
}