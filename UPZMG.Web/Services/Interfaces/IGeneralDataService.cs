using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IGeneralDataService
{
    Task<IReadOnlyList<LookupOption>> GetCareersAsync();
    Task<IReadOnlyList<LookupOption>> GetAsignaturesAsync(Guid careerId);
    Task<IReadOnlyList<PeriodLookupOption>> GetPeriodsAsync();
    Task<IReadOnlyList<LookupOption>> GetMaxDegreesAsync();
    Task<IReadOnlyList<LookupOption>> GetContractTypesAsync();
    Task<IReadOnlyList<LookupOption>> GetInternalAreasAsync();
    Task<IReadOnlyList<LookupOption>> GetTeachersAsync();
    Task<IReadOnlyList<LookupOption>> GetFacilitiesAsync();

    // IReadOnlyList<StudentListItemModel> GetStudentsList();
    // IReadOnlyList<TeacherListItemModel> GetTeachersList();
    // IReadOnlyList<InfrastructureListItemModel> GetInfrastructureList();
    // IReadOnlyList<ExtensionListItemModel> GetExtensionList();
    // IReadOnlyList<FinanceListItemModel> GetFinanceList();
}
