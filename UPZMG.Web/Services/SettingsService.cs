using UPZMG.Web.Models.Settings;

namespace UPZMG.Web.Services;

/// <summary>
/// Settings module data (institution master data, academic periods,
/// configurable indicators). Mock; singleton so added rows persist in-session.
/// </summary>
public interface ISettingsService
{
    InstitutionSettingsViewModel GetInstitution();
    IReadOnlyList<SelectOption> SostenimientoOptions { get; }

    IReadOnlyList<PeriodItemModel> GetPeriods();

    /// <summary>Adds a period. Returns false if the code already exists.</summary>
    bool AddPeriod(string code, string period, string schoolCycle);

    /// <summary>Makes one period the active one, deactivating every other.</summary>
    void ActivatePeriod(string code);

    /// <summary>The single active period, if one is set.</summary>
    PeriodItemModel? ActivePeriod { get; }

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

    private readonly List<PeriodItemModel> _periods = new()
    {
        new() { Code = "2026-EA", Period = "Ene-Abr", SchoolCycle = "2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30), IsActive = true },
        new() { Code = "2026-MA", Period = "May-Ago", SchoolCycle = "2026", StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 8, 31), IsActive = false },
        new() { Code = "2026-SD", Period = "Sep-Dic", SchoolCycle = "2026", StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2026, 12, 31), IsActive = false },
    };

    public IReadOnlyList<PeriodItemModel> GetPeriods() => _periods;

    public PeriodItemModel? ActivePeriod => _periods.FirstOrDefault(p => p.IsActive);

    public bool AddPeriod(string code, string period, string schoolCycle)
    {
        // The period code identifies every capture and report downstream, so a
        // duplicate would silently split one cycle's data across two rows.
        if (_periods.Any(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)))
            return false;

        _periods.Insert(0, new PeriodItemModel
        {
            Code = code,
            Period = period,
            SchoolCycle = schoolCycle,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(4),
            IsActive = false,
        });
        return true;
    }

    /// <summary>
    /// Exactly one period may be active: the whole system ("periodo activo" on
    /// the dashboard, capture forms, report headers) resolves data through it,
    /// so activating one must deactivate the rest in the same operation.
    /// </summary>
    public void ActivatePeriod(string code)
    {
        foreach (var period in _periods)
            period.IsActive = string.Equals(period.Code, code, StringComparison.OrdinalIgnoreCase);
    }

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
