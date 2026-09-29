using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Intelligence;

public class AcademicHistoryBuilderTests
{
    [Fact]
    public void Enrolment_CountsStudentsNotSubjectRows()
    {
        var f = new AcademicFixture();
        var p = f.Periods(1);
        var a = f.Student(p[0]);
        var b = f.Student(p[0]);
        f.Enrol(a, p[0], subjects: 6);
        f.Enrol(b, p[0], subjects: 6);

        var period = Assert.Single(AcademicHistoryBuilder.Build(f.Build()).Periods);

        Assert.Equal(2, period.Enrolled);
    }

    [Fact]
    public void Periods_CarryOutcomesPassRateAndNewStudents()
    {
        var f = new AcademicFixture();
        var p = f.Periods(2);
        var stays = f.Student(p[0]);
        var leaves = f.Student(p[0]);
        var graduates = f.Student(p[0]);
        var arrives = f.Student(p[1]);
        foreach (var s in new[] { stays, leaves, graduates }) f.Enrol(s, p[0]);
        f.Enrol(stays, p[1]);
        f.Enrol(leaves, p[1], FinalStatus.BajaDefinitiva);
        f.Enrol(graduates, p[1], FinalStatus.Egresado);
        f.Enrol(arrives, p[1]);
        f.Grade(stays, p[1], 9, times: 3);
        f.Grade(leaves, p[1], 4, passed: false);

        var history = AcademicHistoryBuilder.Build(f.Build());

        Assert.Equal(2, history.Periods.Count);
        Assert.Equal(3, history.Periods[0].NewEnrolled);
        var second = history.Periods[1];
        Assert.Equal(4, second.Enrolled);
        Assert.Equal(1, second.NewEnrolled);
        Assert.Equal(1, second.Withdrawn);
        Assert.Equal(1, second.Graduated);
        Assert.Equal(75, second.PassRate, precision: 9);
        Assert.Equal(25, second.WithdrawalRate, precision: 9);
    }

    [Fact]
    public void PeriodsWithoutRegistrations_AreSkipped_AndLaterOnesAreUpcoming()
    {
        var f = new AcademicFixture();
        var p = f.Periods(5);
        var s = f.Student(p[0]);
        f.Enrol(s, p[0]);
        f.Enrol(s, p[2]);

        var history = AcademicHistoryBuilder.Build(f.Build());

        Assert.Equal(new[] { "2020 Ene-Abr", "2020 Sep-Dic" }, history.Periods.Select(x => x.Label));
        Assert.Equal(new[] { "2021 Ene-Abr", "2021 May-Ago" }, history.UpcomingLabels);

        // Two periods draw a trend, but the projection must not claim a measured fit.
        Assert.True(history.CanForecast);
        var forecast = IntelligenceEngine.Forecast(history, 1);
        Assert.True(forecast.HasModel);
        Assert.Null(forecast.R2);
        Assert.Null(forecast.NextMargin);
        Assert.Empty(forecast.Upper);
    }

    [Fact]
    public void AnEmptySnapshot_HasNoHistory()
    {
        var history = AcademicHistoryBuilder.Build(AcademicSnapshot.Empty);

        Assert.Empty(history.Periods);
        Assert.Empty(history.UpcomingLabels);
    }

    [Fact]
    public void CountFirstEnrolments_CountsEachStudentOnceInTheirEarliestPeriod()
    {
        Guid p1 = Guid.NewGuid(), p2 = Guid.NewGuid(), p3 = Guid.NewGuid(), unknown = Guid.NewGuid();
        Guid a = Guid.NewGuid(), b = Guid.NewGuid();

        var counts = AcademicHistoryBuilder.CountFirstEnrolments(
            [(a, p3), (a, p2), (b, p3), (b, unknown), (a, p3)],
            [p1, p2, p3]);

        Assert.Equal(1, counts[p2]);
        Assert.Equal(1, counts[p3]);
        Assert.False(counts.ContainsKey(p1));
        Assert.False(counts.ContainsKey(unknown));
    }

    [Theory]
    [InlineData("2025 Ene-Abr", "2025 May-Ago")]
    [InlineData("2025 May-Ago", "2025 Sep-Dic")]
    [InlineData("2025 Sep-Dic", "2026 Ene-Abr")]
    [InlineData("2025-2", null)]
    [InlineData("2025 Verano", null)]
    public void NextLabel_FollowsTheFourMonthCalendar(string label, string? expected)
    {
        Assert.Equal(expected, AcademicHistoryBuilder.NextLabel(label));
    }
}
