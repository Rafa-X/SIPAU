using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

/// <summary>A value/label pair for dropdowns (replaces MVC SelectListItem in Blazor).</summary>
public sealed record SelectOption(string Value, string Text);

/// <summary>
/// Data-capture module data. Demo areas + mock catalogs; swap for real
/// DB/API queries later. Student submissions are mocked (no persistence yet).
/// </summary>
public interface IDataCaptureService
{
    DataCaptureIndexViewModel GetIndex();
    IReadOnlyList<SelectOption> Careers { get; }
    IReadOnlyList<SelectOption> Periods { get; }
    IReadOnlyList<string> Genders { get; }
    IReadOnlyList<string> MaritalStatuses { get; }

    // Catalogs for the other capture forms
    IReadOnlyList<SelectOption> InternalAreas { get; }
    IReadOnlyList<string> MaxDegrees { get; }
    IReadOnlyList<string> ContractTypes { get; }
    IReadOnlyList<string> SpaceUseTypes { get; }
    IReadOnlyList<string> Sectors { get; }
    IReadOnlyList<string> CompanySizes { get; }
    IReadOnlyList<string> ActivityTypes { get; }
    IReadOnlyList<SelectOption> BulkAreas { get; }
    IReadOnlyList<SelectOption> Delimiters { get; }

    /// <summary>Mock student save (no-op placeholder for a future DB/API write).</summary>
    void SaveStudent(StudentFormViewModel model);
}

public sealed class DataCaptureService : IDataCaptureService
{
    private const string ActivePeriodLabel = "Ciclo 2025-2026 (Cuatrimestre 1)";

    public DataCaptureIndexViewModel GetIndex() => new()
    {
        ActivePeriod = ActivePeriodLabel,
        Areas = new List<CaptureAreaModel>
        {
            new() { Id = 1, AreaName = "Alumnos", ResponsiblePerson = "Lic. Ana Martínez", Percentage = 82, Status = "En progreso", LastUpdated = DateTime.Now.AddDays(-1), IsCaptureAvailable = true, ActionName = "Students" },
            new() { Id = 2, AreaName = "Docentes / RRHH", ResponsiblePerson = "Lic. Carlos López", Percentage = 100, Status = "Completo", LastUpdated = DateTime.Now.AddDays(-3), IsCaptureAvailable = true, ActionName = "Teachers" },
            new() { Id = 3, AreaName = "Infraestructura", ResponsiblePerson = "Ing. Roberto Díaz", Percentage = 45, Status = "En progreso", LastUpdated = DateTime.Now.AddDays(-5), IsCaptureAvailable = true, ActionName = "Infrastructure" },
            new() { Id = 4, AreaName = "Extensión y Vinculación", ResponsiblePerson = "Mtro. Pedro Sánchez", Percentage = 30, Status = "Pendiente", LastUpdated = DateTime.Now.AddDays(-8), IsCaptureAvailable = true, ActionName = "Extension" },
            new() { Id = 5, AreaName = "Finanzas", ResponsiblePerson = "C.P. María García", Percentage = 100, Status = "Completo", LastUpdated = DateTime.Now.AddDays(-2), IsCaptureAvailable = true, ActionName = "Finance" },
        },
    };

    public IReadOnlyList<SelectOption> Careers { get; } = new List<SelectOption>
    {
        new("1", "Ingeniería en Sistemas Computacionales"),
        new("2", "Ingeniería Industrial"),
        new("3", "Ingeniería en Biotecnología"),
        new("4", "Licenciatura en Administración y Gestión Empresarial"),
    };

    public IReadOnlyList<SelectOption> Periods { get; } = new List<SelectOption>
    {
        new("1", "2025-2026 / Cuatrimestre 1"),
        new("2", "2025-2026 / Cuatrimestre 2"),
        new("3", "2024-2025 / Cuatrimestre 3"),
    };

    public IReadOnlyList<string> Genders { get; } = new[] { "Masculino", "Femenino", "Otro" };

    public IReadOnlyList<string> MaritalStatuses { get; } = new[] { "Soltero/a", "Casado/a", "Unión libre", "Otro" };

    public IReadOnlyList<SelectOption> InternalAreas { get; } = new List<SelectOption>
    {
        new("1", "Rectoría"),
        new("2", "Dirección Académica"),
        new("3", "Servicios Escolares"),
        new("4", "Recursos Humanos"),
        new("5", "Planeación"),
    };

    public IReadOnlyList<string> MaxDegrees { get; } = new[] { "Licenciatura", "Especialidad", "Maestría", "Doctorado" };

    public IReadOnlyList<string> ContractTypes { get; } = new[] { "Tiempo completo", "Medio tiempo", "Asignatura / Honorarios" };

    public IReadOnlyList<string> SpaceUseTypes { get; } = new[] { "Aula", "Laboratorio", "Taller", "Oficina", "Biblioteca", "Área común" };

    public IReadOnlyList<string> Sectors { get; } = new[] { "Público", "Privado", "Social" };

    public IReadOnlyList<string> CompanySizes { get; } = new[] { "Micro", "Pequeña", "Mediana", "Grande" };

    public IReadOnlyList<string> ActivityTypes { get; } = new[] { "Estadía", "Servicio social", "Prácticas profesionales", "Proyecto de vinculación" };

    public IReadOnlyList<SelectOption> BulkAreas { get; } = new List<SelectOption>
    {
        new("students", "Alumnos"),
        new("teachers", "Docentes / RRHH"),
        new("infrastructure", "Infraestructura"),
        new("extension", "Extensión y Vinculación"),
        new("finance", "Finanzas"),
    };

    public IReadOnlyList<SelectOption> Delimiters { get; } = new List<SelectOption>
    {
        new(",", "Coma ( , )"),
        new(";", "Punto y coma ( ; )"),
        new("tab", "Tabulación ( Tab )"),
        new("|", "Barra vertical ( | )"),
    };

    public void SaveStudent(StudentFormViewModel model)
    {
        // Mock: nothing is persisted yet. A real implementation would call the API/DB here.
    }
}
