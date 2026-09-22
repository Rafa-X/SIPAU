namespace UPZMG.Implementations.DataCapture;

public sealed record LookupOption(Guid Id, string Value, string? Alias = null);

public sealed record PeriodLookupOption(Guid Id, string Period, string SchoolCycle, string? IdCode = null)
{
	public string Value => $"{SchoolCycle} {Period}";
}