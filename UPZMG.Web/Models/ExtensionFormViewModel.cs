using Microsoft.AspNetCore.Mvc.Rendering;

namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the extension / linkage project form (mock, no real persistence).
/// Mirrors key fields of extension.convenios + alumnos_proyectos_vinculacion from the SIPAU design.
/// </summary>
public class ExtensionFormViewModel
{
    public string OrganizationName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string CompanySize { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string StudentEnrollmentNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int PeriodId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int AccreditedHours { get; set; }

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> Periods { get; set; } = new();
}
