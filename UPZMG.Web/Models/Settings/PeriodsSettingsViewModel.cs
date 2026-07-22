namespace UPZMG.Web.Models.Settings;

/// <summary>
/// ViewModel for the periods (ciclos escolares) management page (mock, no real persistence).
/// Mirrors academic.periodos from the SIGIE design.
/// </summary>
public class PeriodsSettingsViewModel
{
    public List<PeriodItemModel> Periods { get; set; } = new();
}

/// <summary>
/// Represents a single academic period row.
/// </summary>
public class PeriodItemModel
{
    public string Code { get; set; } = string.Empty;
    public string SchoolCycle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}
