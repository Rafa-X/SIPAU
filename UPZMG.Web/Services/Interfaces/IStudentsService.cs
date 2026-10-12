using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Charts;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface IStudentsService
{
    Task<Student?> CreateStudent(CreateStudent request);

    /// <summary>The captured student, to fill the correction form; null when it no longer exists.</summary>
    Task<UpdateStudent?> GetStudentAsync(Guid studentId);

    Task<Student?> UpdateStudent(UpdateStudent request);
    Task<StudentsListResponse> GetStudentsListAsync(StudentsListRequest request);
    // Null when the request failed; an empty list means there is nothing to count.
    Task<List<StudentsAges>?> GetStudentsAgesAsync(bool newOnly = false);
}