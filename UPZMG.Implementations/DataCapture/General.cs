namespace UPZMG.Implementations.DataCapture;

public sealed record LookupOption(Guid Id, string Value);

public sealed record PeriodLookupOption(Guid Id, string Period, string SchoolCycle)
{
	public string Value => $"{SchoolCycle} {Period}";
}