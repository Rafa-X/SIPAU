using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using UPZMG.Persistence;
using UPZMG.Web.Models;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Controllers;

[Authorize]
public class ReportsController(IWebHostEnvironment environment, AppDBContext db, IMemoryCache cache) : Controller
{
    private static readonly TimeSpan TemplateCacheDuration = TimeSpan.FromMinutes(10);
    private const string TemplateCatalogCacheKey = "reports-template-catalog";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

#region Index
    [HttpGet]
    public async Task<IActionResult> Index(ReportsOverviewViewModel? reportsModel = null)
    {
        reportsModel ??= new ReportsOverviewViewModel();
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : Guid.Empty;

        if (userId != Guid.Empty)
        {
            reportsModel.AvailableTemplates = await GetTemplatesForSelection();

            // TODO: move this to a service layer to avoid direct DB access in the controller
            var recentReports = await db.Reports
                .AsNoTracking()
                .Where(x => x.CreatedBy == userId)
                .OrderByDescending(x => x.LastModified)
                .Select(report => new
                {
                    report.Id,
                    report.ReportType,
                    report.TemplateVersion,
                    report.Status,
                    report.LastModified
                })
                .Take(5)
                .ToListAsync();

            var templateLookup = reportsModel.AvailableTemplates
                .ToDictionary(
                    t => $"{t.ReportType}|{t.Version}",
                    t => t.DisplayName, StringComparer.OrdinalIgnoreCase);

            reportsModel.LoadedReports = recentReports
                .Select(report =>
                {
                    return new ReportTemplateViewModel
                    {
                        Id = report.Id,
                        ReportType = Enum.TryParse<ReportType>(report.ReportType, ignoreCase: true, out var parsedReportType) ? parsedReportType : default,
                        DisplayName = templateLookup.GetValueOrDefault($"{report.ReportType}|{report.TemplateVersion}", report.ReportType),
                        TemplateVersion = report.TemplateVersion ?? string.Empty,
                        Status = Enum.TryParse<ReportStatus>(report.Status, out var parsedStatus) ? parsedStatus : default,
                        LastModified = report.LastModified,
                    };
                })
                .ToList();
        }

        ViewData["SelectedReportType"] = reportsModel.ReportType;
        return View(reportsModel);
    }
#endregion

#region LoadTemplate
    [HttpGet]
    public async Task<IActionResult> LoadTemplate(ReportTemplateViewModel reportModel)
    {
        Guid userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : Guid.Empty;
        if (userId == Guid.Empty) 
            return Unauthorized("No se pudo determinar el usuario actual.");

        Reports? report = new();

        if (reportModel.Id != Guid.Empty)
        {
            // Case for loading an existing Report by Id
            report = await db.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == reportModel.Id && x.CreatedBy == userId);

            if (report is null) 
                return NotFound($"No se encontro reporte con Id {reportModel.Id} para el usuario actual.");

            reportModel.Id = report.Id;
            reportModel.TemplateVersion = report.TemplateVersion ?? reportModel.TemplateVersion;
            reportModel.Status = Enum.TryParse<ReportStatus>(report.Status, out var parsedStatus) ? parsedStatus : reportModel.Status;
            reportModel.Answers = ParseAnswersDictionary(report.AnswersJson);
        }

        var templateVersion = !string.IsNullOrWhiteSpace(report?.TemplateVersion)
            ? report.TemplateVersion
            : reportModel.TemplateVersion;
        reportModel.Template = await GetTemplateByTypeAndVersionAsync(reportModel.ReportType.ToString(), templateVersion) 
            ?? null;

        // By this point if the template is still null, it means it was not found in the cache or in the file system
        if (reportModel.Template is null)
            return NotFound($"No se encontro template para {reportModel.ReportType} version {templateVersion}.");

        ViewData["SelectedTemplateFile"] = $"{reportModel.ReportType}-{templateVersion}.json";
        return View(reportModel);
    }
#endregion

#region SaveDraft
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(ReportTemplateViewModel model, string submitAction, string sessionAnswersJson)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : Guid.Empty;
        if (userId == Guid.Empty)
            return Unauthorized("No se pudo determinar el usuario actual.");

        var templateVersion = model.TemplateVersion;
        model.Template = await GetTemplateByTypeAndVersionAsync(model.ReportType.ToString(), templateVersion) 
            ?? null;

        if (string.IsNullOrWhiteSpace(templateVersion) || model.Template is null)
            return BadRequest("No se pudo determinar la version o el template no fue encontrado.");
        
        var report = new Reports
        {
            Id = Guid.NewGuid(),
            CreatedBy = userId,
            ReportType = model.ReportType.ToString(),
            TemplateVersion = templateVersion,
            Status = submitAction.ToString(),
            AnswersJson = "{}",
            DateCreated = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        report.AnswersJson = MergeAnswers(
            MergeAnswers(report.AnswersJson, sessionAnswersJson ?? "{}"),
            ExtractAnswersJson(Request.Form));
        db.Reports.Add(report); // Add the new report to the database context

        if (submitAction == ReportStatus.draft.ToString())
            TempData["ReportStatusMessage"] = "Borrador guardado correctamente.";

        if (submitAction == ReportStatus.submitted.ToString())
            TempData["ReportStatusMessage"] = "Reporte terminado y guardado correctamente.";

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
#endregion


#region Helper Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<ReportTemplateDefinition>> GetTemplatesForSelection()
    {
        var templates = await cache.GetOrCreateAsync(TemplateCatalogCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TemplateCacheDuration;

            var templateDirectory = Path.Combine(environment.ContentRootPath, "Views", "Reports", "Templates");
            var templateFiles = Directory.GetFiles(templateDirectory, "*.json", SearchOption.TopDirectoryOnly);
            var catalog = new List<ReportTemplateDefinition>();

            foreach (var file in templateFiles)
            {
                var rawJson = await System.IO.File.ReadAllTextAsync(file);
                var template = JsonSerializer.Deserialize<ReportTemplateDefinition>(rawJson, JsonOptions);
                if (template != null)
                {
                    catalog.Add(template);
                }
            }

            return (IReadOnlyList<ReportTemplateDefinition>)catalog
                .OrderBy(t => t.ReportType)
                .ThenBy(t => t.Version)
                .ToArray();
        });

        return templates?.ToList() ?? [];
    }

    private async Task<ReportTemplateDefinition?> GetTemplateByTypeAndVersionAsync(string reportType, string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        var cacheKey = $"reports-template:{reportType}:{version}";
        var rawJson = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TemplateCacheDuration;
            var templateFilePath = Path.Combine(environment.ContentRootPath, "Views", "Reports", "Templates", $"{reportType}_{version}.json");

            if (!System.IO.File.Exists(templateFilePath))
                return null;

            return await System.IO.File.ReadAllTextAsync(templateFilePath);
        });

        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        var template = JsonSerializer.Deserialize<ReportTemplateDefinition>(rawJson, JsonOptions);
        return template;
    }

    private static string MergeAnswers(string currentAnswersJson, string newAnswersJson)
    {
        var mergedAnswers = ParseAnswersDictionary(currentAnswersJson);

        foreach (var pair in ParseAnswersDictionary(newAnswersJson))
        {
            mergedAnswers[pair.Key] = pair.Value;
        }

        return JsonSerializer.Serialize(mergedAnswers, JsonOptions);
    }

    private static Dictionary<string, string?> ParseAnswersDictionary(string? answersJson)
    {
        if (string.IsNullOrWhiteSpace(answersJson))
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(answersJson, JsonOptions);
        if (parsed is null)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, string?>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    private static string ExtractAnswersJson(IFormCollection form)
    {
        var answers = form
            .Where(pair => pair.Key.StartsWith("answers[", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return JsonSerializer.Serialize(answers, JsonOptions);
    }
#endregion
}
