using UPZMG.Implementations.DataCapture;
using UPZMG.Implementations.Institution;
using UPZMG.Persistence.Models;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

public interface IGeneralDataService
{
    Task<IReadOnlyList<LookupOption>> GetCareersSelectableAsync();
    Task<IReadOnlyList<LookupOption>> GetCoursesAsync(Guid careerId);
    Task<IReadOnlyList<PeriodLookupOption>> GetPeriodsAsync();
    Task<IReadOnlyList<LookupOption>> GetMaxDegreesAsync();
    Task<IReadOnlyList<LookupOption>> GetContractTypesAsync();
    Task<IReadOnlyList<LookupOption>> GetInternalAreasAsync();
    Task<IReadOnlyList<LookupOption>> GetTeachersAsync();
    Task<IReadOnlyList<LookupOption>> GetFacilitiesAsync();
    Task<IReadOnlyList<LookupOption>> GetIndigenousLanguagesAsync();

    /// <summary>Institution master data from the 911.10 capture; never null.</summary>
    Task<InstitutionProfile> GetInstitutionAsync();

}
