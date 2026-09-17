namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the annual financial summary form (mock, no real persistence).
/// Mirrors key fields of finanzas.finanzas_anuales from the SIPAU design.
/// </summary>
public class FinanceFormViewModel
{
    public int FiscalYear { get; set; }

    // Income by source
    public decimal FederalIncome { get; set; }
    public decimal StateIncome { get; set; }
    public decimal OwnIncomeEnrollment { get; set; }
    public decimal OwnIncomeServices { get; set; }

    // Expenses by type
    public decimal PayrollExpense { get; set; }
    public decimal MaterialsExpense { get; set; }
    public decimal ServicesExpense { get; set; }
    public decimal InvestmentExpense { get; set; }

    public string Notes { get; set; } = string.Empty;
}
