using Microsoft.AspNetCore.Mvc.Rendering;
using UPZMG.Shared.Enums;

namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the employee / staff (RRHH) registration form (mock, no real persistence).
/// Mirrors key fields of the rrhh.empleados table from the SIGIE design.
/// </summary>
public class EmployeeFormViewModel
{
    public int EmployeeNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string Curp { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public EducationLevel HighestEducationLevel { get; set; }
    public TitleStatus TitleStatus { get; set; }
    public IReadOnlyCollection<string> IndigenousLanguages { get; set; } = [];
    public bool HasDisability { get; set; }
    public int ExternalXPYears { get; set; }
    public DateTime? FirstHiredDate { get; set; }
    
    public string MaxDegree { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public Guid? AreaId { get; set; }
    public int WeeklyHours { get; set; }
    public bool HasPostgraduate { get; set; }

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> Areas { get; set; } = new();
}
