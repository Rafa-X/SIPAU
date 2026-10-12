using Microsoft.Data.SqlClient;
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

    /// <summary>
    /// Makes one period the active one. Two administrators doing this at the same time each
    /// used to read the period that was active, clear that one and set their own, which could
    /// leave two periods marked active — and "the active period" is read all over the system.
    /// Written as two statements inside a transaction instead of through the change tracker:
    /// the clear sees whatever is active at that moment, and a filtered unique index refuses
    /// a second active row if anything still slips through.
    /// </summary>
    public async Task<bool> ActivatePeriodAsync(ActivatePeriod request)
    {
        if (!await db.Periods.AnyAsync(p => p.Id == request.PeriodId)) return false;

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Cleared before set, so the transaction never holds two active rows at once.
            await db.Periods
                .Where(p => p.IsActive && p.Id != request.PeriodId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.IsActive, false));

            await db.Periods
                .Where(p => p.Id == request.PeriodId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.IsActive, true));

            await transaction.CommitAsync();
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException(
                "Otro usuario cambió el periodo activo al mismo tiempo. Vuelve a intentarlo.");
        }
    }
}
