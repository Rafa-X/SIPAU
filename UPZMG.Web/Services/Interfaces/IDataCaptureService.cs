using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IDataCaptureService
{
    IReadOnlyList<string> ContractTypes { get; }
    IReadOnlyList<string> SpaceUseTypes { get; }
    IReadOnlyList<string> Sectors { get; }
    IReadOnlyList<string> CompanySizes { get; }
    IReadOnlyList<string> ActivityTypes { get; }
    IReadOnlyList<SelectOption> BulkAreas { get; }
    IReadOnlyList<SelectOption> Delimiters { get; }

    Task<Student?> CreateStudent(CreateStudent request);
    Task<Employee?> CreateTeacher(CreateEmployee request);
    Task<ClassGroups?> CreateClassGroup(CreateClassGroup request);

    Task<IReadOnlyList<TeacherListItem>> GetTeachersListAsync();
}
