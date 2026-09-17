using UPZMG.Implementations.Charts;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Services;

public interface IChartsService
{
    Task<List<ChartCategory>> GetCareersDistributionAsync();
}