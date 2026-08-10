using System.Text.Json.Serialization;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Models;

public class ReportTemplateDefinition
{
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName("reportType")]
    public string ReportType { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = ReportStatus.draft.ToString();

    [JsonPropertyName("sections")]
    public List<ReportTemplateSection> Sections { get; set; } = [];
}

public class ReportTemplateSection
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("questions")]
    public List<ReportTemplateQuestion> Questions { get; set; } = [];
}

public class ReportTemplateQuestion
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("helpText")]
    public string? HelpText { get; set; }

    [JsonPropertyName("rows")]
    public List<string> Rows { get; set; } = [];

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = [];
}

public class ReportsOverviewViewModel
{
    public List<ReportTemplateDefinition> AvailableTemplates { get; set; } = [];
    public List<ReportTemplateViewModel> LoadedReports { get; set; } = [];
    public ReportType? ReportType { get; set; }
    public string TemplateVersion { get; set; } = string.Empty;
}

public class ReportTemplateViewModel
{
    public Guid Id { get; set; } = Guid.Empty;
    public ReportType ReportType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public int SectionIndex { get; set; } = 0;
    public DateTime LastModified { get; set; }
    public ReportTemplateDefinition? Template { get; set; }
    public Dictionary<string, string?> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}


public enum ReportType
{
    Escuela911,
    Institucion911,
    Infraestructura911,
    Carrera911,
    AnexoTI,
    FormatoSII,
}