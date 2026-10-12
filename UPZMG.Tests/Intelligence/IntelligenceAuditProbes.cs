using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Intelligence;

// Probes from the 2026-09-28 analysis of the intelligent layer. Each one described something
// the model got wrong; they now state what it does instead.
public class IntelligenceAuditProbes
{
    /// <summary>
    /// Leaving for a period, coming back and graduating is a graduation. It used to be read as
    /// a withdrawal: the first state that was not "Activo" ended the trajectory, and the guard
    /// against that only protected students who are still enrolled today.
    /// </summary>
    [Fact]
    public void TemporaryLeaveFollowedByGraduationIsAGraduation()
    {
        var f = new AcademicFixture();
        var p = f.Periods(4);
        var student = f.Student(p[0], StudentStatus.Egresado);
        f.Enrol(student, p[0]); f.Grade(student, p[0], 8);
        f.Enrol(student, p[1], FinalStatus.BajaTemporal);
        f.Enrol(student, p[2]); f.Grade(student, p[2], 9);
        f.Enrol(student, p[3], FinalStatus.Egresado);
        Assert.False(Assert.Single(TrajectoryBuilder.Build(f.Build())).Outcome);
    }

    /// <summary>
    /// Two terminal states in one period no longer depend on the order the rows arrive in:
    /// graduating and withdrawing cannot both have happened, and graduation is what did.
    /// </summary>
    [Fact]
    public void MixedStatusesInOnePeriodResolveTheSameWayInAnyOrder()
    {
        var f = new AcademicFixture();
        var p = f.Periods(2);
        var student = f.Student(p[0], StudentStatus.Egresado);
        f.Enrol(student, p[0]); f.Grade(student, p[0], 8);
        f.Enrol(student, p[1], FinalStatus.BajaTemporal, subjects: 1);
        f.Enrol(student, p[1], FinalStatus.Egresado, subjects: 1);
        var snapshot = f.Build();
        Assert.False(Assert.Single(TrajectoryBuilder.Build(snapshot)).Outcome);
        Assert.False(Assert.Single(TrajectoryBuilder.Build(snapshot with { Registrations = snapshot.Registrations.Reverse().ToList() })).Outcome);
    }

    /// <summary>
    /// A period nobody has graded has no pass rate at all. It used to report zero, and every
    /// screen that draws the complement turned that into "100 % de reprobación".
    /// </summary>
    [Fact]
    public void UngradedPeriodHasNoPassRateInsteadOfZero()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1);
        var student = f.Student(p[0]);
        f.Enrol(student, p[0]);
        var history = Assert.Single(AcademicHistoryBuilder.Build(f.Build()).Periods);
        Assert.Equal(0, history.Passed + history.Failed);
        Assert.False(history.HasGrades);
        Assert.Null(history.PassRate);
    }

    /// <summary>
    /// A decline steep enough to send the trend below zero used to produce an upper bound
    /// underneath the lower one, because only the lower edge was held at zero.
    /// </summary>
    [Fact]
    public void FallingForecastKeepsItsBandInOrder()
    {
        var history = new AcademicHistory(new[] { 30, 20, 10 }.Select((n, i) =>
            new PeriodStats(Guid.NewGuid(), $"P{i}", new DateOnly(2025, i + 1, 1), n, 0, 0, 0, 0, 0)).ToList(), []);
        var forecast = IntelligenceEngine.Forecast(history, 2);
        Assert.Equal(0, forecast.Actual[^1]);
        Assert.True(forecast.Upper[^1] >= forecast.Lower[^1]);
        Assert.All(forecast.Upper, v => Assert.True(v >= 0));
    }

    /// <summary>
    /// New entrants come from the entry period on the student record. Taking the earliest
    /// enrolment the data happened to show reported the whole enrolment of the first loaded
    /// period as new students.
    /// </summary>
    [Fact]
    public void TheStartOfTheDataWindowIsNotCountedAsNewEntrants()
    {
        var f = new AcademicFixture();
        var p = f.Periods(2);
        var student = f.Student(p[0]);
        f.Enrol(student, p[1]);
        var history = Assert.Single(AcademicHistoryBuilder.Build(f.Build()).Periods);
        Assert.Equal(p[1], history.PeriodId);
        Assert.Equal(0, history.NewEnrolled);
    }

    /// <summary>
    /// A título grade counts as an extra opportunity, which is why the input is no longer
    /// called "extraordinarios": the name claimed something narrower than what it counts.
    /// </summary>
    [Fact]
    public void TitleCountsAsAnExtraOpportunity()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1);
        var student = f.Student(p[0]);
        f.Enrol(student, p[0]);
        f.Grade(student, p[0], 8, opportunity: Opportunity.Titulo);
        Assert.Equal(1, Assert.Single(TrajectoryBuilder.Build(f.Build())).Retakes);
    }
}
