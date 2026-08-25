namespace UPZMG.Implementations;

public sealed class GetReportRequest
{
	public Guid ReportId { get; init; }
	public string? TemplateVersion { get; init; }
}
