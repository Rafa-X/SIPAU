using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IGeneralDataService
{
    Task<IReadOnlyList<Careers>> GetCareersAsync();
    Task<IReadOnlyList<LookupOption>> GetCareersSelectableAsync();
    Task<IReadOnlyList<LookupOption>> GetAsignaturesAsync(Guid careerId);
    Task<IReadOnlyList<PeriodLookupOption>> GetPeriodsAsync();
    Task<IReadOnlyList<LookupOption>> GetMaxDegreesAsync();
    Task<IReadOnlyList<LookupOption>> GetContractTypesAsync();
    Task<IReadOnlyList<LookupOption>> GetInternalAreasAsync();
    Task<IReadOnlyList<LookupOption>> GetTeachersAsync();
    Task<IReadOnlyList<LookupOption>> GetFacilitiesAsync();

}
