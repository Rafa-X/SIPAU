using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IDataCaptureService
{
    DataCaptureIndexViewModel GetIndex();

    IReadOnlyList<SelectOption> Careers { get; }
    IReadOnlyList<SelectOption> Periods { get; }
    IReadOnlyList<SelectOption> InternalAreas { get; }
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

    IReadOnlyList<TeacherListItemModel> GetTeachersList();
    IReadOnlyList<InfrastructureListItemModel> GetInfrastructureList();
    IReadOnlyList<ExtensionListItemModel> GetExtensionList();
    IReadOnlyList<FinanceListItemModel> GetFinanceList();
}
