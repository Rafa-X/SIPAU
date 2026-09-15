using System.ComponentModel.DataAnnotations;

namespace UPZMG.Implementations.Settings;

/// <summary>An academic period as the settings screen and the dashboard read it.</summary>
public sealed record PeriodItem(
    Guid Id,
    string Code,
    string Period,
    string SchoolCycle,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive)
{
    public string Value => $"{SchoolCycle} {Period}";
}

public sealed class CreatePeriod
{
    public required Guid RequesterKey { get; set; }
    [MaxLength(20)]
    public required string Code { get; set; }
    [MaxLength(8)]
    public required string Period { get; set; }
    // periods.SchoolCycle holds a four-digit year.
    [MaxLength(4)]
    public required string SchoolCycle { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public sealed class ActivatePeriod
{
    public required Guid RequesterKey { get; set; }
    public required Guid PeriodId { get; set; }
}
