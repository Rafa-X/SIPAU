using UPZMG.Implementations.Charts;
using UPZMG.Implementations.Dashboard;
using UPZMG.Web.Components.Shared.Charts;
using UPZMG.Web.Services;

using UPZMG.Implementations.Intelligence;

namespace UPZMG.Web.Models.Charts;

public sealed class ChartsPageViewModel
{
    public EnrollmentChartModel Enrollment { get; private init; } = new();
    public CategoryChartModel CareerDistribution { get; set; } = new();
    public GroupDistributionChartModel GroupDistributionByCareer { get; set; } = new();
}

public sealed class EnrollmentChartModel
{
    /// <summary>True when the API could not be reached, which is not the same as having no data.</summary>
    public bool Failed { get; set; }

    /// <summary>What the projection got wrong on periods it had not seen; null until measurable.</summary>
    public ForecastAccuracy? Accuracy { get; set; }

    public IReadOnlyList<string> Labels { get; set; } = [];
    public IReadOnlyList<ChartSeriesData> Series { get; set; } = [];
    public int ForecastFromIndex { get; set; }
    public double Slope { get; set; }
    public double Intercept { get; set; }
    // False when there is too little history to fit the regression.
    public bool HasModel { get; set; }
    // Null with two observed periods: any line fits them exactly, so the fit is not measurable.
    public double? R2 { get; set; }
    public string? NextLabel { get; set; }
    public double? NextValue { get; set; }
    public double? NextMargin { get; set; }
}

public sealed class CategoryChartModel
{
    public List<string> Labels { get; set; } = [];
    public List<ChartSeriesData> Series { get; set; } = [];
    public string TopLabel { get; set; } = "Sin datos";
    public double Total { get; set; }
}

public sealed class GroupDistributionChartModel
{
    public List<string> Labels { get; set; } = [];
    public List<ChartSeriesData> Series { get; set; } = [];
    public string TopLabel { get; set; } = "Sin datos";
}

public sealed class ChartsStudentsModel
{
    public ChartStudentsAges NewStudents { get; set; } = new();
    public ChartStudentsAges AllStudents { get; set; } = new();
    public List<ChartSeriesData> NewStudentsAges { get; set; } = [];
    public List<ChartSeriesData> AllStudentsAges { get; set; } = [];
}