using UPZMG.Implementations.Charts;
using UPZMG.Implementations.DataCapture;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface IChartsService
{
    Task<List<ChartCategory>> GetCareersDistributionAsync();
    Task<List<GroupDistributionCareer>> GetGroupsDistributionByCareerAsync();

    Task<TeachersDistribution?> GetTeachersDistributionAsync();

    /// <summary>Active students by programme and sex; null when the API did not answer.</summary>
    Task<GenderDistribution?> GetGenderDistributionAsync();

    /// <summary>Grants of the active period by source; null when the API did not answer.</summary>
    Task<ScholarshipsBySource?> GetScholarshipsBySourceAsync();

    /// <summary>Titled students against graduates; null when the API did not answer.</summary>
    Task<TitulationSummary?> GetTitulationSummaryAsync();
}