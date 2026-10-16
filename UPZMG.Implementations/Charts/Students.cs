namespace UPZMG.Implementations.Charts;

public sealed class StudentsAges
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<double> Values { get; set; } = [];
    public string Color { get; set; } = string.Empty;
}

public sealed class ChartStudentsAges
{
    public IReadOnlyList<string> Labels { get; set; } = [];
    public IReadOnlyList<double> Values { get; set; } = [];
}

public sealed record GroupDistributionCareer(string Career, int Groups, int Students);

/// <summary>Teaching staff counted by highest degree and by seniority.</summary>
public sealed record TeachersDistribution(
    int Teachers,
    IReadOnlyList<ChartCategory> ByDegree,
    IReadOnlyList<ChartCategory> BySeniority);
/// <summary>Active students by programme, split by sex, as the 911 and SII formats require.</summary>
public sealed record GenderDistribution(
    IReadOnlyList<string> Careers,
    IReadOnlyList<int> Women,
    IReadOnlyList<int> Men)
{
    public int TotalWomen => Women.Sum();
    public int TotalMen => Men.Sum();
}
