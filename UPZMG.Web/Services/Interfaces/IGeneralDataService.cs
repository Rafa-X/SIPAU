using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IGeneralDataService
{
    Task<IReadOnlyList<LookupOption>> GetCareersAsync();
    Task<IReadOnlyList<LookupOption>> GetPeriodsAsync();
    Task<IReadOnlyList<LookupOption>> GetMaxDegreesAsync();
    Task<IReadOnlyList<LookupOption>> GetContractTypesAsync();
    Task<IReadOnlyList<LookupOption>> GetInternalAreasAsync();

    // IReadOnlyList<StudentListItemModel> GetStudentsList();
    // IReadOnlyList<TeacherListItemModel> GetTeachersList();
    // IReadOnlyList<InfrastructureListItemModel> GetInfrastructureList();
    // IReadOnlyList<ExtensionListItemModel> GetExtensionList();
    // IReadOnlyList<FinanceListItemModel> GetFinanceList();
}
