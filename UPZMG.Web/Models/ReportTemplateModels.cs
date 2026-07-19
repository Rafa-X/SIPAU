using System.Text.Json.Serialization;

namespace UPZMG.Web.Models;

public class ReportTemplateDefinition
{
    [JsonPropertyName("reportKey")]
    public string ReportKey { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

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

public class ReportsTemplateViewModel
{
    public ReportTemplateDefinition Template { get; set; } = null!;
    public ReportType? ReportType { get; set; }
    public Guid? DraftId { get; set; }
    public string TemplateVersion { get; set; } = string.Empty;
    public int SectionIndex { get; set; } = -1;
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