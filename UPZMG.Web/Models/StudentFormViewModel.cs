using Microsoft.AspNetCore.Mvc.Rendering;

namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the student registration form (mock, no real persistence).
/// </summary>
public class StudentFormViewModel
{
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string Curp { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int CareerId { get; set; }
    public int PeriodId { get; set; }
    public bool SpeaksIndigenousLanguage { get; set; }
    public string MaritalStatus { get; set; } = string.Empty;

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> Careers { get; set; } = new();
    public List<SelectListItem> Periods { get; set; } = new();
}
