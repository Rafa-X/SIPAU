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
    Task<List<StudentsAges>> GetStudentsAgesAsync(bool newOnly = false);
}