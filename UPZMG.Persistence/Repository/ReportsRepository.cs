using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class ReportsRepository
{
    public async Task<List<Reports>> GetRecentReportsByUserAsync(AppDBContext db, Guid userId, int take = 5)
    {
        if (userId == Guid.Empty)
            return [];

        return await db.Reports
            .AsNoTracking()
            .Where(report => report.CreatedBy == userId)
            .OrderByDescending(report => report.LastModified)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Reports?> GetReportByIdForUserAsync(AppDBContext db, Guid reportId, Guid userId)
    {
        if (reportId == Guid.Empty || userId == Guid.Empty)
            return null;

        return await db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(report => report.Id == reportId && report.CreatedBy == userId);
    }
}