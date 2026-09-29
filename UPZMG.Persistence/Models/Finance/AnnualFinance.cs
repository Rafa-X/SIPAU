namespace UPZMG.Persistence.Models;

/// <summary>
/// The annual financial summary the institution reports: income by source and expenditure by
/// type, one row per fiscal year. Amounts are decimal, never floating point: a budget that
/// rounds differently each time it is added is a budget nobody can sign.
/// </summary>
public class AnnualFinance
{
    public Guid Id { get; set; }

    /// <summary>Fiscal year; one summary per year.</summary>
    public int FiscalYear { get; set; }

    public decimal FederalIncome { get; set; }
    public decimal StateIncome { get; set; }
    public decimal OwnIncomeEnrollment { get; set; }
    public decimal OwnIncomeServices { get; set; }

    public decimal PayrollExpense { get; set; }
    public decimal MaterialsExpense { get; set; }
    public decimal ServicesExpense { get; set; }
    public decimal InvestmentExpense { get; set; }

    public string? Notes { get; set; }

    public Guid CreatedBy { get; set; } //fk to systemUser
    public DateTime DateCreated { get; set; }
    public DateTime LastModified { get; set; }

    public decimal TotalIncome => FederalIncome + StateIncome + OwnIncomeEnrollment + OwnIncomeServices;

    public decimal TotalExpense => PayrollExpense + MaterialsExpense + ServicesExpense + InvestmentExpense;
}
