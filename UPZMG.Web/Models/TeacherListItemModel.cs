namespace UPZMG.Web.Models;

/// <summary>
/// Represents a single row in the captured-teachers listing.
/// </summary>
public class TeacherListItemModel
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime CapturedOn { get; set; }
}
