namespace UPZMG.Implementations.Charts;

public sealed record ChartCategory(string Label, int Value);

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