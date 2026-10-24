using System.ComponentModel.DataAnnotations;
using UPZMG.Shared.Enums;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Finance module.
/// Represents the financial module in the academic system.
/// </summary>

public class Finance
{
    public Guid Id { get; set; }
    public int FiscalYear { get; set; }
    public double FederalIncome { get; set; }
    public double StateIncome { get; set; }
    public double InscriptionsIncome { get; set; }
    public double ServicesIncome { get; set; }
    public double OtherIncome { get; set; }
    public double PayrollExpense { get; set; }
    public double MaterialsExpense { get; set; }
    public double ServicesExpense { get; set; }
    public double InvestmentExpense { get; set; }
    public double OperationalExpenseTeaching { get; set; }
    public double OperationalExpenseResearch { get; set; }
    public double OperationalExpenseExtension { get; set; }
    public double OperationalExpenseAdministration { get; set; }
    public string Notes { get; set; } = string.Empty;
}