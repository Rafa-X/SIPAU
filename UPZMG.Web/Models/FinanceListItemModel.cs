namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the captured-finance listing.
/// </summary>
public class FinanceListItemModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime CapturedOn { get; set; }
}
