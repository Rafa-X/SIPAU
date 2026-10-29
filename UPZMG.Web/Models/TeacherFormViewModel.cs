using Microsoft.AspNetCore.Mvc.Rendering;
using UPZMG.Shared.Enums;

namespace UPZMG.Web.Models;

/// <summary>
/// Form state for capturing or correcting an employee. It mirrors CreateEmployee, with the
/// nullable dates and the name split into three inputs.
/// Mirrors key fields of the rrhh.empleados table from the SIPAU design.
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
    public bool IsTeacher { get; set; } = true;

    // Dropdown options, filled from the catalogues the API serves.
    public List<SelectListItem> Areas { get; set; } = new();
}
