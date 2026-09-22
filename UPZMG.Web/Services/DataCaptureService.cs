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
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

  
    public IReadOnlyList<string> ContractTypes { get; } = new[] { "Tiempo completo", "Medio tiempo", "Asignatura / Honorarios" };

    public IReadOnlyList<string> SpaceUseTypes { get; } = new[] { "Aula", "Laboratorio", "Taller", "Oficina", "Biblioteca", "Área común" };

    public IReadOnlyList<string> Sectors { get; } = new[] { "Público", "Privado", "Social" };

    public IReadOnlyList<string> CompanySizes { get; } = new[] { "Micro", "Pequeña", "Mediana", "Grande" };

    public IReadOnlyList<string> ActivityTypes { get; } = new[] { "Estadía", "Servicio social", "Prácticas profesionales", "Proyecto de vinculación" };

    public IReadOnlyList<SelectOption> BulkAreas { get; } = new List<SelectOption>
    {
        new("students", "Alumnos"),
        new("teachers", "Docentes / RRHH"),
        new("classgroups", "Grupos de clase"),
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
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar al alumno.");
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
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar al empleado.");
    }

    public async Task<ClassGroups?> CreateClassGroup(CreateClassGroup request)
    {
        var response = await securityService.SendPostRequestAsync<CreateClassGroup, ClassGroups>(
            "data-capture/class-groups/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<ClassGroups>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar el grupo de clase.");
    }

#endregion


#region Listings
    // Teaching staff already captured; empty when the area cannot read them.
    public async Task<IReadOnlyList<TeacherListItem>> GetTeachersListAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<IReadOnlyList<TeacherListItem>>(
                "data-capture/employees/teachers", requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<List<TeacherListItem>>(response.Result, ResponseOptions) ?? [];
            }
        }
        catch (HttpRequestException)
        {
            // The listing is limited to Admin and Recursos Humanos.
        }
        return [];
    }

    // Captured class groups; empty when the area cannot read them.
    public async Task<IReadOnlyList<ClassGroupListItem>> GetClassGroupsListAsync()
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<IReadOnlyList<ClassGroupListItem>>(
                "data-capture/class-groups/list", requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<List<ClassGroupListItem>>(response.Result, ResponseOptions) ?? [];
            }
        }
        catch (HttpRequestException)
        {
            // The listing is limited to Admin and Dirección Académica.
        }
        return [];
    }
#endregion
}
