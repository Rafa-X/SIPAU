namespace UPZMG.Web.Models;

/// <summary>
/// ViewModel for the physical space / TIC registration form (mock, no real persistence).
/// Mirrors key fields of recursos.espacios_fisicos + inventario_tic_anual from the SIGIE design.
/// </summary>
public class InfrastructureFormViewModel
{
    public string SpaceName { get; set; } = string.Empty;
    public string UseType { get; set; } = string.Empty;
    public decimal SquareMeters { get; set; }
    public int Capacity { get; set; }
    public int ComputersInOperation { get; set; }
    public bool HasInternet { get; set; }
    public bool HasComputerEquipment { get; set; }
    public bool IsAdapted { get; set; }
}
