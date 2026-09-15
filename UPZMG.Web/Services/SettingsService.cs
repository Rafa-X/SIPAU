using UPZMG.Web.Models.Settings;

namespace UPZMG.Web.Services;

/// <summary>
/// Settings module data (institution master data, configurable indicators).
/// Mock; singleton so added rows persist in-session. Academic periods are
/// real data and live in <see cref="PeriodsService"/>.
/// </summary>
public interface ISettingsService
{
    InstitutionSettingsViewModel GetInstitution();
    IReadOnlyList<SelectOption> SostenimientoOptions { get; }

    IReadOnlyList<IndicatorItemModel> GetIndicators();
    void AddIndicator(string reportSheet, string name, string dataType);
    IReadOnlyList<string> IndicatorDataTypes { get; }
}

public sealed class SettingsService : ISettingsService
{
    public IReadOnlyList<SelectOption> SostenimientoOptions { get; } = new List<SelectOption>
    {
        new("Publico_Federal", "Público Federal"),
        new("Publico_Estatal", "Público Estatal"),
        new("Privado", "Privado"),
    };

    public IReadOnlyList<string> IndicatorDataTypes { get; } = new[] { "Número", "Porcentaje", "Texto", "Booleano" };

    public InstitutionSettingsViewModel GetInstitution() => new()
    {
        InstitutionName = "Universidad Politécnica de la Zona Metropolitana de Guadalajara",
        InstitutionKey = "UPZMG",
        CampusName = "Campus Tlajomulco",
        CctKey = "14EPO0001X",
        RectorName = "Dr. Hassem Macías",
        Sostenimiento = "Publico_Estatal",
        Phone = "33 1234 5678",
        Website = "https://upzmg.edu.mx",
        ResponsibleName = "Mtro. Francisco (Subdirección de Planeación)",
        ResponsiblePosition = "Subdirector de Planeación, Evaluación y Calidad",
        ResponsibleEmail = "planeacion@upzmg.edu.mx",
        Street = "Av. Universidad",
        StreetNumber = "1000",
        Neighborhood = "Centro",
        Municipality = "Tlajomulco de Zúñiga",
        State = "Jalisco",
        PostalCode = "45640",
    };

    private readonly List<IndicatorItemModel> _indicators = new()
    {
        new() { ReportSheet = "911.9A — Matrícula", Name = "Alumnos de nuevo ingreso", DataType = "Número", IsActive = true },
        new() { ReportSheet = "911.9A — Matrícula", Name = "Índice de aprovechamiento", DataType = "Porcentaje", IsActive = true },
        new() { ReportSheet = "SII — Alumnos", Name = "Alumnos con beca federal", DataType = "Número", IsActive = true },
        new() { ReportSheet = "Anexo TIC", Name = "Computadoras con internet", DataType = "Número", IsActive = true },
        new() { ReportSheet = "SII — Alumnos", Name = "Alumnos que trabajan", DataType = "Booleano", IsActive = false },
    };

    public IReadOnlyList<IndicatorItemModel> GetIndicators() => _indicators;

    public void AddIndicator(string reportSheet, string name, string dataType) =>
        _indicators.Insert(0, new IndicatorItemModel { ReportSheet = reportSheet, Name = name, DataType = dataType, IsActive = true });
}
