using System.Net;
using System.Text.Json;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public sealed class StudentsService(
    ISecurityService securityService) : IStudentsService
{
    private static readonly JsonSerializerOptions ResponseOptions = new(JsonSerializerDefaults.Web);

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

    public async Task<StudentsListResponse> GetStudentsListAsync(StudentsListRequest request)
    {
        var query = new List<string>
        {
            $"page={request.Page}",
            $"pageSize={request.PageSize}",
        };
        if (!string.IsNullOrWhiteSpace(request.Name)) query.Add($"name={Uri.EscapeDataString(request.Name)}");
        if (!string.IsNullOrWhiteSpace(request.EnrollmentNumber)) query.Add($"enrollmentNumber={Uri.EscapeDataString(request.EnrollmentNumber)}");
        if (request.CareerId.HasValue) query.Add($"careerId={request.CareerId.Value}");
        if (request.PeriodId.HasValue) query.Add($"periodId={request.PeriodId.Value}");

        var requesterKey = await securityService.GetRequesterKeyAsync();
        var response = await securityService.SendGetRequestAsync<StudentsListResponse>(
            $"data-capture/students/list?{string.Join('&', query)}", requesterKey);

        if (response.StatusCode == HttpStatusCode.OK && response.Result is not null)
        {
            return JsonSerializer.Deserialize<StudentsListResponse>(response.Result, ResponseOptions) ?? new StudentsListResponse();
        }
        return new StudentsListResponse();
    }

    // No backing API endpoints exist yet for these; nothing in the app calls them today.
    public Task<Student?> GetStudentById(Guid studentId) => throw new NotImplementedException();
    public Task<List<Student>> GetAllStudents() => throw new NotImplementedException();
    public Task<bool> DeleteStudent(Guid studentId) => throw new NotImplementedException();
}