using System.Net;
using System.Text.Json;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

// A value/label pair for dropdowns (replaces MVC SelectListItem in Blazor)
public sealed record SelectOption(string Value, string Text);

public sealed class DataCaptureService(ISecurityService securityService): IDataCaptureService
{
    private const string ActivePeriodLabel = "Ciclo 2025-2026 (Cuatrimestre 1)";
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

    public DataCaptureIndexViewModel GetIndex() => new()
    {
        ActivePeriod = ActivePeriodLabel,
        Areas = new List<DataCaptureAreaModel>
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
        new("11111111-1111-1111-1111-111111111111", "Ingeniería en Sistemas Computacionales"),
        new("11111111-1111-1111-1111-111111111113", "Ingeniería Industrial"),
        new("11111111-1111-1111-1111-111111111112", "Ingeniería en Biotecnología"),
    };

    public IReadOnlyList<SelectOption> Periods { get; } = new List<SelectOption>
    {
        new("22222222-2222-2222-2222-222222222201", "2025-A / Semestre 1"),
        new("22222222-2222-2222-2222-222222222202", "2025-B / Semestre 2"),
    };

    public IReadOnlyList<SelectOption> InternalAreas { get; } = new List<SelectOption>
    {
        new("1", "Rectoría"),
        new("2", "Dirección Académica"),
        new("3", "Servicios Escolares"),
        new("4", "Recursos Humanos"),
        new("5", "Planeación"),
    };

  
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

#region Student
    public async Task<Student?> CreateStudent(CreateStudent request)
    {
        var response = await securityService.SendPostRequestAsync<CreateStudent, Student>(
            "data-capture/students/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Student>(response.Result, ResponseOptions) ?? null;
        }
        return null;
    }
#endregion

#region Teacher
    public async Task<Employee?> CreateTeacher(CreateEmployee request)
    {
        var response = await securityService.SendPostRequestAsync<CreateEmployee, Employee>(
            "data-capture/employees/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Employee>(response.Result, ResponseOptions);
        }
        return null;
    }

#endregion


#region TODO
    // TODO: replace with real API-backed queries once list endpoints exist for these entities.
    public IReadOnlyList<TeacherListItemModel> GetTeachersList() => new List<TeacherListItemModel>
    {
        new() { Id = 1, EmployeeNumber = "E-1001", Name = "Lic. Carlos López", Area = InternalAreas[1].Text, ContractType = ContractTypes[0], Period = Periods[0].Text, CapturedOn = DateTime.Now.AddDays(-3) },
        new() { Id = 2, EmployeeNumber = "E-1002", Name = "Mtra. Diana Cruz", Area = InternalAreas[3].Text, ContractType = ContractTypes[1], Period = Periods[0].Text, CapturedOn = DateTime.Now.AddDays(-7) },
        new() { Id = 3, EmployeeNumber = "E-1003", Name = "Ing. Roberto Díaz", Area = InternalAreas[2].Text, ContractType = ContractTypes[2], Period = Periods[1].Text, CapturedOn = DateTime.Now.AddDays(-11) },
    };

    public IReadOnlyList<InfrastructureListItemModel> GetInfrastructureList() => new List<InfrastructureListItemModel>
    {
        new() { Id = 1, Name = "Aula 101", UseType = SpaceUseTypes[0], Capacity = 40, Period = Periods[0].Text, CapturedOn = DateTime.Now.AddDays(-5) },
        new() { Id = 2, Name = "Laboratorio de Cómputo 1", UseType = SpaceUseTypes[1], Capacity = 30, Period = Periods[0].Text, CapturedOn = DateTime.Now.AddDays(-10) },
        new() { Id = 3, Name = "Biblioteca central", UseType = SpaceUseTypes[4], Capacity = 80, Period = Periods[1].Text, CapturedOn = DateTime.Now.AddDays(-15) },
    };

    public IReadOnlyList<ExtensionListItemModel> GetExtensionList() => new List<ExtensionListItemModel>
    {
        new() { Id = 1, Name = "Estadía profesional Sistemas", OrganizationName = "Grupo Industrial GRIN", ActivityType = ActivityTypes[0], Period = Periods[0].Text, CapturedOn = DateTime.Now.AddDays(-8) },
        new() { Id = 2, Name = "Servicio social comunitario", OrganizationName = "DIF Municipal", ActivityType = ActivityTypes[1], Period = Periods[1].Text, CapturedOn = DateTime.Now.AddDays(-14) },
    };

    public IReadOnlyList<FinanceListItemModel> GetFinanceList() => new List<FinanceListItemModel>
    {
        new() { Id = 1, Name = $"Resumen financiero {DateTime.Now.Year}", TotalIncome = 12_500_000m, TotalExpense = 11_800_000m, Period = Periods[0].Text, CapturedOn = DateTime.Now.AddDays(-2) },
        new() { Id = 2, Name = $"Resumen financiero {DateTime.Now.Year - 1}", TotalIncome = 11_900_000m, TotalExpense = 11_400_000m, Period = Periods[2].Text, CapturedOn = DateTime.Now.AddDays(-200) },
    };
#endregion
}
