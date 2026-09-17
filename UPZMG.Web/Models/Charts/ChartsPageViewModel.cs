using UPZMG.Implementations.Charts;
using UPZMG.Implementations.Dashboard;
using UPZMG.Web.Components.Shared.Charts;
using UPZMG.Web.Services;

namespace UPZMG.Web.Models.Charts;

public sealed class ChartsPageViewModel
{
    public EnrollmentChartModel Enrollment { get; private init; } = new();
    public CategoryChartModel CareerDistribution { get; set; } = new();
}

public sealed class EnrollmentChartModel
{
    public IReadOnlyList<string> Labels { get; set; } = [];
    public IReadOnlyList<ChartSeriesData> Series { get; set; } = [];
    public int ForecastFromIndex { get; set; }
    public double Slope { get; set; }
    public double Intercept { get; set; }
}

public sealed class CategoryChartModel
{
    public List<string> Labels { get; set; } = [];
    public List<ChartSeriesData> Series { get; set; } = [];
    public string TopLabel { get; set; } = "Sin datos";
    public double Total { get; set; }
}

public sealed class ChartsStudentsModel
{
    public ChartStudentsAges NewStudents { get; set; } = new();
    public ChartStudentsAges AllStudents { get; set; } = new();
    public List<ChartSeriesData> NewStudentsAges { get; set; } = [];
    public List<ChartSeriesData> AllStudentsAges { get; set; } = [];
}