using System.Text.Json;
using UPZMG.Web.Models;

namespace UPZMG.Web.Services;

/// <summary>
/// Reports 911 module: loads the versioned JSON template definitions that drive
/// the dynamic capture form. Template files live in Views/Reports/Templates.
/// (Draft persistence is mocked in the UI for now.)
/// </summary>
public interface IReportsService
{
    IReadOnlyList<ReportOption> AvailableReports { get; }
    Task<ReportTemplateDefinition?> LoadTemplateAsync(ReportType type);
}

public sealed record ReportOption(ReportType Type, string Label);

public sealed class ReportsService : IReportsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Dictionary<ReportType, string> TemplateMap = new()
    {
        [ReportType.AnexoTI] = "sep_911_anexo_tic",
    };

    private readonly IWebHostEnvironment _env;

    public ReportsService(IWebHostEnvironment env) => _env = env;

    public IReadOnlyList<ReportOption> AvailableReports { get; } = new List<ReportOption>
    {
        new(ReportType.AnexoTI, "Anexo Tecnologías de la Información"),
        new(ReportType.Carrera911, "Carrera 911"),
        new(ReportType.FormatoSII, "Formato SII"),
    };

    public async Task<ReportTemplateDefinition?> LoadTemplateAsync(ReportType type)
    {
        if (!TemplateMap.TryGetValue(type, out var reportKey))
            return null;

        var dir = Path.Combine(_env.ContentRootPath, "Views", "Reports", "Templates");
        if (!Directory.Exists(dir)) return null;

        var matches = Directory.GetFiles(dir, $"{reportKey}.*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0) return null;

        var json = await File.ReadAllTextAsync(matches[0]);
        return JsonSerializer.Deserialize<ReportTemplateDefinition>(json, JsonOptions);
    }
}
