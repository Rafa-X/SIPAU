using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Intelligence;

// The feature engineering, and above all its leakage guards.
public class TrajectoryBuilderTests
{
    [Fact]
    public void Graduate_IsANegativeOutcome_AndActive_HasNone()
    {
        var f = new AcademicFixture();
        var p = f.Periods(3);
        var graduate = f.Student(p[0], StudentStatus.Egresado);
        f.Enrol(graduate, p[0]); f.Grade(graduate, p[0], 9);
        f.Enrol(graduate, p[1], FinalStatus.Egresado); f.Grade(graduate, p[1], 9);
        var active = f.Student(p[1]);
        f.Enrol(active, p[1]); f.Grade(active, p[1], 8);

        var byId = TrajectoryBuilder.Build(f.Build()).ToDictionary(o => o.StudentId);

        Assert.False(byId[graduate].Outcome);
        Assert.Null(byId[active].Outcome);
    }

    [Fact]
    public void GradesFromTheOutcomePeriod_AreLeftOut()
    {
        // Every failure happened in the period the student left: abandoned courses.
        var f = new AcademicFixture();
        var p = f.Periods(2);
        var student = f.Student(p[0], StudentStatus.BajaDefinitiva);
        f.Enrol(student, p[0]); f.Grade(student, p[0], 9, times: 5);
        f.Enrol(student, p[1], FinalStatus.BajaDefinitiva); f.Grade(student, p[1], 4, passed: false, times: 5);

        var observation = Assert.Single(TrajectoryBuilder.Build(f.Build()));

        Assert.True(observation.Outcome);
        Assert.Equal(0, observation.FailureRate);
        Assert.Equal(9, observation.AverageGrade);
    }

    [Fact]
    public void AStudentWithNoGradesBeforeTheOutcome_IsNotAnObservation()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1);
        var student = f.Student(p[0], StudentStatus.BajaTemporal);
        f.Enrol(student, p[0], FinalStatus.BajaTemporal); f.Grade(student, p[0], 5, passed: false);

        Assert.Empty(TrajectoryBuilder.Build(f.Build()));
    }

    [Fact]
    public void AStudentWithoutRegistrations_IsSkipped()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1);
        f.Student(p[0]);

        Assert.Empty(TrajectoryBuilder.Build(f.Build()));
    }

    [Fact]
    public void EveryoneIsMeasuredOnTheSameRecentWindow()
    {
        // A long record and a short one produce windows of the same size.
        var f = new AcademicFixture();
        var p = f.Periods(6);
        var longRecord = f.Student(p[0]);
        for (int i = 0; i < 6; i++)
        {
            f.Enrol(longRecord, p[i]);
            f.Grade(longRecord, p[i], i < 6 - TrajectoryBuilder.RecentPeriods ? 5 : 9,
                passed: i >= 6 - TrajectoryBuilder.RecentPeriods);
        }
        var shortRecord = f.Student(p[5]);
        f.Enrol(shortRecord, p[5]); f.Grade(shortRecord, p[5], 7);

        var byId = TrajectoryBuilder.Build(f.Build()).ToDictionary(o => o.StudentId);

        Assert.Equal(TrajectoryBuilder.RecentPeriods, byId[longRecord].PeriodsObserved);
        Assert.Equal(9, byId[longRecord].AverageGrade);
        Assert.Equal(0, byId[longRecord].FailureRate);
        Assert.Equal(1, byId[shortRecord].PeriodsObserved);
    }

    [Fact]
    public void ActiveStudents_UseTheirLatestGradedPeriod()
    {
        var f = new AcademicFixture();
        var p = f.Periods(3);
        var student = f.Student(p[0]);
        f.Enrol(student, p[0]); f.Grade(student, p[0], 6);
        f.Enrol(student, p[1]); f.Grade(student, p[1], 10);
        // Registered in the current period, but nothing graded yet.
        f.Enrol(student, p[2]);

        var observation = Assert.Single(TrajectoryBuilder.Build(f.Build()));

        Assert.Equal(Math.Min(2, TrajectoryBuilder.RecentPeriods), observation.PeriodsObserved);
        Assert.Equal(TrajectoryBuilder.RecentPeriods == 1 ? 10 : 8, observation.AverageGrade);
    }

    [Fact]
    public void RatesAreComputedOverAttempts()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1);
        var student = f.Student(p[0], works: true);
        f.Enrol(student, p[0]);
        f.Grade(student, p[0], 8, times: 3);
        f.Grade(student, p[0], 5, passed: false);
        f.Grade(student, p[0], 7, opportunity: Opportunity.Extraordinario);

        var o = Assert.Single(TrajectoryBuilder.Build(f.Build()));

        Assert.Equal((8 * 3 + 5 + 7) / 5.0, o.AverageGrade, precision: 9);
        Assert.Equal(20, o.FailureRate, precision: 9);
        Assert.Equal(1, o.Retakes);
        Assert.Equal(1, o.RetakesPerPeriod, precision: 9);
        Assert.Equal(1, o.Features[DropoutFeatures.Works]);
        Assert.Equal(
            new[] { DropoutFeatures.Average, DropoutFeatures.FailureRate, DropoutFeatures.RetakesPerPeriod, DropoutFeatures.EntryAge, DropoutFeatures.Works },
            o.Features.Keys);
    }

    [Fact]
    public void AgeIsTakenAtEntry_NotToday()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1, firstYear: 2015);
        // Starts 2015-01-01 a day before turning 19.
        var student = f.Student(p[0], birthDate: new DateOnly(1996, 1, 2));
        f.Enrol(student, p[0]); f.Grade(student, p[0], 8);

        var observation = Assert.Single(TrajectoryBuilder.Build(f.Build()));

        Assert.Equal(18, observation.EntryAge);
    }

    [Fact]
    public void TheStudentRecordCanCarryTheOutcome()
    {
        // No registration says so, but Control Escolar marked the withdrawal on the student.
        var f = new AcademicFixture();
        var p = f.Periods(3);
        var student = f.Student(p[0], StudentStatus.BajaDefinitiva);
        f.Enrol(student, p[0]); f.Grade(student, p[0], 9);
        f.Enrol(student, p[1]); f.Grade(student, p[1], 4, passed: false);

        var observation = Assert.Single(TrajectoryBuilder.Build(f.Build()));

        Assert.True(observation.Outcome);
        // The last registered period is the outcome period, so its grades are left out.
        Assert.Equal(9, observation.AverageGrade);
    }

    [Fact]
    public void ATemporaryLeaveFollowedByAReturn_IsStillActive()
    {
        var f = new AcademicFixture();
        var p = f.Periods(3);
        var student = f.Student(p[0]);
        f.Enrol(student, p[0]); f.Grade(student, p[0], 8);
        f.Enrol(student, p[1], FinalStatus.BajaTemporal);
        f.Enrol(student, p[2]); f.Grade(student, p[2], 9);

        var observation = Assert.Single(TrajectoryBuilder.Build(f.Build()));

        Assert.Null(observation.Outcome);
        Assert.Equal(TrajectoryBuilder.RecentPeriods == 1 ? 9 : 8.5, observation.AverageGrade);
    }

    [Theory]
    [InlineData("2000-06-15", "2018-06-14", 17)]
    [InlineData("2000-06-15", "2018-06-15", 18)]
    [InlineData("2000-02-29", "2019-02-28", 19)]   // a leap-day birthday counts on 28 February
    [InlineData("2030-01-01", "2020-01-01", 0)]
    public void AgeAt_CountsCompletedYears(string birth, string on, int expected)
    {
        Assert.Equal(expected, TrajectoryBuilder.AgeAt(DateOnly.Parse(birth), DateOnly.Parse(on)));
    }
}
