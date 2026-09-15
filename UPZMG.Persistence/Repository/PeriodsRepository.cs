using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Settings;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

public class PeriodsRepository(AppDBContext db)
{
    public async Task<List<PeriodItem>> GetPeriodsAsync() =>
        await db.Periods
            .AsNoTracking()
            .OrderBy(p => p.StartDate)
            .Select(p => new PeriodItem(
                p.Id,
                p.IdCode,
                p.SEP_Period,
                p.SchoolCycle,
                p.StartDate,
                p.EndDate,
                p.IsActive))
            .ToListAsync();

    public async Task<PeriodItem> CreatePeriodAsync(CreatePeriod request)
    {
        var code = request.Code.Trim();
        var exist = await db.Periods.AnyAsync(p => p.IdCode == code);
        if (exist)
            throw new InvalidOperationException("A period with the same code already exists.");

        // New periods start inactive; activating one is a separate, explicit step.
        var period = new Periods
        {
            Id = Guid.NewGuid(),
            IdCode = code,
            SEP_Period = request.Period.Trim(),
            SchoolCycle = request.SchoolCycle.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = false,
        };

        db.Periods.Add(period);
        await db.SaveChangesAsync();

        return new PeriodItem(
            period.Id,
            period.IdCode,
            period.SEP_Period,
            period.SchoolCycle,
            period.StartDate,
            period.EndDate,
            period.IsActive);
    }

    public async Task<bool> ActivatePeriodAsync(ActivatePeriod request)
    {
        var periods = await db.Periods.ToListAsync();
        if (periods.All(p => p.Id != request.PeriodId))
            return false;

        // One SaveChanges keeps the switch atomic: never zero or two active periods.
        foreach (var period in periods)
            period.IsActive = period.Id == request.PeriodId;

        await db.SaveChangesAsync();
        return true;
    }
}
