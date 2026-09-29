using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// The annual financial summary, one row per fiscal year. Saving the same year twice updates it
/// rather than creating a second budget: the institution has one set of figures per year.
/// </summary>
public class FinanceRepository(AppDBContext db)
{
    public async Task<AnnualFinance> SaveAsync(SaveAnnualFinance request, Guid savedBy)
    {
        var now = DateTime.UtcNow;
        var summary = await db.AnnualFinances.FirstOrDefaultAsync(f => f.FiscalYear == request.FiscalYear);

        if (summary is null)
        {
            summary = new AnnualFinance
            {
                Id = Guid.NewGuid(),
                FiscalYear = request.FiscalYear,
                CreatedBy = savedBy,
                DateCreated = now,
            };
            db.AnnualFinances.Add(summary);
        }

        summary.FederalIncome = request.FederalIncome;
        summary.StateIncome = request.StateIncome;
        summary.OwnIncomeEnrollment = request.OwnIncomeEnrollment;
        summary.OwnIncomeServices = request.OwnIncomeServices;
        summary.PayrollExpense = request.PayrollExpense;
        summary.MaterialsExpense = request.MaterialsExpense;
        summary.ServicesExpense = request.ServicesExpense;
        summary.InvestmentExpense = request.InvestmentExpense;
        summary.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        summary.LastModified = now;

        await db.SaveGuardingUniquenessAsync("Ya existe un resumen financiero para ese ejercicio.");
        return summary;
    }

    public async Task<List<FinanceListItem>> GetListAsync() =>
        await db.AnnualFinances.AsNoTracking()
            .OrderByDescending(f => f.FiscalYear)
            .Select(f => new FinanceListItem(
                f.Id,
                f.FiscalYear,
                f.FederalIncome,
                f.StateIncome,
                f.OwnIncomeEnrollment,
                f.OwnIncomeServices,
                f.PayrollExpense,
                f.MaterialsExpense,
                f.ServicesExpense,
                f.InvestmentExpense,
                f.Notes,
                f.LastModified))
            .ToListAsync();
}
