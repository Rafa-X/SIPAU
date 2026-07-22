namespace UPZMG.Web.Models.Settings;

/// <summary>
/// ViewModel for the indicators management page (mock, no real persistence).
/// Implements the client requirement that an admin can append new indicators
/// to a report sheet each year without a developer.
/// </summary>
public class IndicatorsSettingsViewModel
{
    public List<IndicatorItemModel> Indicators { get; set; } = new();
}

/// <summary>
/// Represents a single configurable indicator attached to a report sheet.
/// </summary>
public class IndicatorItemModel
{
    public string ReportSheet { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary> "Número", "Porcentaje", "Texto", "Booleano" </summary>
    public string DataType { get; set; } = "Número";

    public bool IsActive { get; set; } = true;
}
