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
        //new("classgroups", "Grupos de clase"),
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

    public async Task<SchoolFacility?> CreateFacility(CreateFacility request)
    {
        var response = await securityService.SendPostRequestAsync<CreateFacility, SchoolFacility>(
            "data-capture/facilities/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<SchoolFacility>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar el espacio.");
    }

    public async Task<OutreachProject?> CreateOutreachProject(CreateOutreachProject request)
    {
        var response = await securityService.SendPostRequestAsync<CreateOutreachProject, OutreachProject>(
            "data-capture/outreach/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<OutreachProject>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar el proyecto.");
    }

    public async Task<AnnualFinance?> SaveFinance(SaveAnnualFinance request)
    {
        var response = await securityService.SendPostRequestAsync<SaveAnnualFinance, AnnualFinance>(
            "data-capture/finance/save", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<AnnualFinance>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar el resumen financiero.");
    }

    public async Task<UpdateEmployee?> GetTeacherAsync(Guid id) =>
        await GetOneAsync<UpdateEmployee>($"data-capture/employees/{id}");

    public async Task<UpdateClassGroup?> GetClassGroupAsync(Guid id) =>
        await GetOneAsync<UpdateClassGroup>($"data-capture/class-groups/{id}");

    public async Task<Employee?> UpdateTeacher(UpdateEmployee request)
    {
        var response = await securityService.SendPostRequestAsync<UpdateEmployee, Employee>(
            "data-capture/employees/update", JsonSerializer.Serialize(request), request.RequesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<Employee>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar los cambios del docente.");
    }

    public async Task<ClassGroups?> UpdateClassGroup(UpdateClassGroup request)
    {
        var response = await securityService.SendPostRequestAsync<UpdateClassGroup, ClassGroups>(
            "data-capture/class-groups/update", JsonSerializer.Serialize(request), request.RequesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<ClassGroups>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar los cambios del grupo.");
    }

    public async Task<ScolarshipAssigned?> CreateScholarship(CreateScholarship request)
    {
        var response = await securityService.SendPostRequestAsync<CreateScholarship, ScolarshipAssigned>(
            "data-capture/scholarships/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<ScolarshipAssigned>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar la beca.");
    }

    public async Task<TitulationProcess?> CreateTitulation(CreateTitulation request)
    {
        var response = await securityService.SendPostRequestAsync<CreateTitulation, TitulationProcess>(
            "data-capture/titulation/create", JsonSerializer.Serialize(request), request.RequesterKey);

        if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            && response.Result is not null)
        {
            return JsonSerializer.Deserialize<TitulationProcess>(response.Result, ResponseOptions);
        }
        throw new InvalidOperationException(response.ErrorMessage ?? "No fue posible guardar la titulación.");
    }

    // Null when the record is gone or the area may not read it; the screen explains it.
    private async Task<T?> GetOneAsync<T>(string path) where T : class
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<T>(path, requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<T>(response.Result, ResponseOptions);
            }
        }
        catch (HttpRequestException)
        {
        }
        return null;
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

    // School spaces already registered; null when the listing could not be read.
    public Task<IReadOnlyList<FacilityListItem>?> GetFacilitiesListAsync() =>
        ListAsync<FacilityListItem>("data-capture/facilities/list");

    // Linkage projects already registered; null when the listing could not be read.
    public Task<IReadOnlyList<OutreachListItem>?> GetOutreachListAsync() =>
        ListAsync<OutreachListItem>("data-capture/outreach/list");

    // Annual financial summaries; null when the listing could not be read.
    public Task<IReadOnlyList<FinanceListItem>?> GetFinanceListAsync() =>
        ListAsync<FinanceListItem>("data-capture/finance/list");

    // Scholarships already granted; null when the listing could not be read.
    public Task<IReadOnlyList<ScholarshipListItem>?> GetScholarshipsAsync() =>
        ListAsync<ScholarshipListItem>("data-capture/scholarships/list");

    // Titulation processes already registered; null when the listing could not be read.
    public Task<IReadOnlyList<TitulationListItem>?> GetTitulationsAsync() =>
        ListAsync<TitulationListItem>("data-capture/titulation/list");

    // The scholarship programmes the area can choose from; empty when none could be read.
    public async Task<IReadOnlyList<LookupOption>> GetScholarshipProgramsAsync() =>
        await ListAsync<LookupOption>("data-capture/scholarships/programs") ?? [];

    /// <summary>
    /// One listing, or null when it could not be read. Returning an empty list for a refused
    /// or unanswered request made every consultation screen say "sin registros", which is a
    /// statement about the institution's data rather than about the request that failed.
    /// </summary>
    private async Task<IReadOnlyList<T>?> ListAsync<T>(string path)
    {
        var requesterKey = await securityService.GetRequesterKeyAsync();
        try
        {
            var response = await securityService.SendGetRequestAsync<IReadOnlyList<T>>(path, requesterKey);

            if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
            {
                return JsonSerializer.Deserialize<List<T>>(response.Result, ResponseOptions) ?? [];
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

#endregion
}
