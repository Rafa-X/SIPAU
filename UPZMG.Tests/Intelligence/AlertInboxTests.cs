using UPZMG.Implementations.Intelligence;
using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Intelligence;

/// <summary>
/// The alerts are institutional; what each person saw or dismissed is theirs. These cover the
/// part that joins the two, which is where a mark could be lost or applied to the wrong alert.
/// </summary>
public class AlertInboxTests
{
    private static AnomalyAlert Alert(string key, string severity = "warning") =>
        new($"Alerta {key}", "detalle", severity, 2.0, "2026 Ene-Abr", key);

    [Fact]
    public void WithNoReceipts_EveryAlertIsNew()
    {
        var inbox = AlertInboxBuilder.Build([Alert("a"), Alert("b")], []);

        Assert.Equal(2, inbox.Alerts.Count);
        Assert.Equal(2, inbox.Unseen);
        Assert.Equal(0, inbox.Dismissed);
    }

    [Fact]
    public void ASeenAlertStaysListed_AndStopsCounting()
    {
        var inbox = AlertInboxBuilder.Build(
            [Alert("a"), Alert("b")],
            [new AlertReceiptState("a", Seen: true, Dismissed: false)]);

        Assert.Equal(2, inbox.Alerts.Count);
        Assert.True(inbox.Alerts.Single(x => x.Alert.Key == "a").Seen);
        Assert.Equal(1, inbox.Unseen);
    }

    [Fact]
    public void ADismissedAlertLeavesTheList_AndIsCountedSoItCanComeBack()
    {
        var inbox = AlertInboxBuilder.Build(
            [Alert("a"), Alert("b")],
            [new AlertReceiptState("a", Seen: true, Dismissed: true)]);

        Assert.Equal("b", Assert.Single(inbox.Alerts).Alert.Key);
        Assert.Equal(1, inbox.Dismissed);
    }

    [Fact]
    public void AReceiptForADeviationNoLongerDetected_ChangesNothing()
    {
        var inbox = AlertInboxBuilder.Build(
            [Alert("a")],
            [new AlertReceiptState("gone", Seen: true, Dismissed: true)]);

        Assert.Single(inbox.Alerts);
        Assert.Equal(1, inbox.Unseen);
        Assert.Equal(0, inbox.Dismissed);
    }

    [Fact]
    public void TheOrderOfTheAlertsIsKept()
    {
        var inbox = AlertInboxBuilder.Build(
            [Alert("first", "error"), Alert("second"), Alert("third", "info")],
            [new AlertReceiptState("second", Seen: true, Dismissed: false)]);

        Assert.Equal(["first", "second", "third"], inbox.Alerts.Select(x => x.Alert.Key));
    }

    [Fact]
    public void EveryEducationLevelReadsAsWords()
    {
        foreach (var level in Enum.GetValues<EducationLevel>())
        {
            var label = level.Label();

            // A label that is still the member name ("MediaSuperiorOMenor") has an inner capital.
            Assert.False(string.IsNullOrWhiteSpace(label));
            Assert.DoesNotContain(label.Skip(1), char.IsUpper);
        }
    }
}
