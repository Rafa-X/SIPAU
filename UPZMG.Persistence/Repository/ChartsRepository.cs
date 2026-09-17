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
                g.Key.Name ?? "Sin nombre",
                g.Count()
            )
        ).ToListAsync();

        return result.OrderByDescending(x => x.Value).ToList();
    }
}