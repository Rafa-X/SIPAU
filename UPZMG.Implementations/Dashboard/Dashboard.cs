using UPZMG.Implementations.Settings;

namespace UPZMG.Implementations.Dashboard;

/// <summary>
/// Home dashboard figures, counted from captured rows only.
/// Academic indicators (pass rate, dropout, terminal efficiency) belong to the metrics module.
/// </summary>
public sealed class DashboardSummary
{
    public PeriodItem? ActivePeriod { get; set; }

    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    // Students whose first period is the active one.
    public int NewEnrollments { get; set; }

    public int TotalTeachers { get; set; }
    public int TeachersWithPostgrad { get; set; }

    // Class groups opened in the active period.
    public int ClassGroups { get; set; }
    public int GroupCapacity { get; set; }
    public int GroupEnrolled { get; set; }

    public IReadOnlyList<CareerStudents> StudentsByCareer { get; set; } = [];
    public IReadOnlyList<PeriodIntake> IntakeByPeriod { get; set; } = [];
}

public sealed record CareerStudents(string Career, int Students);

public sealed record PeriodIntake(Guid PeriodId, string Period, int Students);
