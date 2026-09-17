using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Charts;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface IStudentsService
{
    Task<Student?> CreateStudent(CreateStudent request);
    Task<Student?> GetStudentById(Guid studentId);
    Task<List<Student>> GetAllStudents();
    //Task<Student?> UpdateStudent(UpdateStudent request);
    Task<bool> DeleteStudent(Guid studentId);
    Task<StudentsListResponse> GetStudentsListAsync(StudentsListRequest request);
    Task<List<StudentsAges>> GetStudentsAgesAsync(bool newOnly = false);
}