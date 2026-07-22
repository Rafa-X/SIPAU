using Microsoft.AspNetCore.Mvc.Rendering;

namespace UPZMG.Web.Models.Settings;

/// <summary>
/// ViewModel for the institutional configuration form (mock, no real persistence).
/// Mirrors key fields of academic.configuracion_institucional from the SIGIE design.
/// These master data feed the headers of every generated report.
/// </summary>
public class InstitutionSettingsViewModel
{
    public string InstitutionName { get; set; } = string.Empty;
    public string InstitutionKey { get; set; } = string.Empty;
    public string CampusName { get; set; } = string.Empty;
    public string CctKey { get; set; } = string.Empty;
    public string RectorName { get; set; } = string.Empty;
    public string Sostenimiento { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Report capture responsible
    public string ResponsibleName { get; set; } = string.Empty;
    public string ResponsiblePosition { get; set; } = string.Empty;
    public string ResponsibleEmail { get; set; } = string.Empty;

    // Address
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    // Dropdown options (mock data, populated by the controller)
    public List<SelectListItem> SostenimientoOptions { get; set; } = new();
}
