using Microsoft.AspNetCore.Mvc.Rendering;

namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the teacher / staff (RRHH) registration form (mock, no real persistence).
/// Mirrors key fields of the rrhh.empleados table from the SIGIE design.
/// </summary>
public class TeacherFormViewModel
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string Curp { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string MaxDegree { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public int WeeklyHours { get; set; }
    public bool HasPostgraduate { get; set; }

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> Areas { get; set; } = new();
}
