namespace UPZMG.Implementations.DataCapture;

public sealed record LookupOption(Guid Id, string Value, string? Alias = null);

/// <param name="IsActive">The period the university is in; capture forms start on it.</param>
public sealed record PeriodLookupOption(Guid Id, string Period, string SchoolCycle, string? IdCode = null, bool IsActive = false)
{
	public string Value => $"{SchoolCycle} {Period}";
}