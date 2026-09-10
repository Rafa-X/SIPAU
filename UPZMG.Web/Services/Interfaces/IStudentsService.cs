using UPZMG.Implementations;
using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Permissions;
using UPZMG.Implementations.Roles;
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
}