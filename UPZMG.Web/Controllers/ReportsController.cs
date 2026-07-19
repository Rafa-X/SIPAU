using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UPZMG.Persistence;
using UPZMG.Web.Models;
using UPZMG.Persistence.Models;

namespace UPZMG.Web.Controllers;

[Authorize]
public class ReportsController(IWebHostEnvironment environment, AppDBContext db) : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string DefaultTemplateVersion = "2024-2025";

    [HttpGet]
    public IActionResult Index(ReportsTemplateViewModel? reportsModel = null)
    {
        reportsModel ??= new ReportsTemplateViewModel();

        if (reportsModel.ReportType.HasValue)
            return RedirectToAction(nameof(LoadTemplate), reportsModel);

        ViewData["SelectedReportType"] = reportsModel.ReportType;
        return View(reportsModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadTemplate(ReportsTemplateViewModel reportsModel)
    {
        if (!reportsModel.ReportType.HasValue)
            return BadRequest("Debe especificarse el tipo de reporte.");

        reportsModel.Template ??= await GetTemplateJsonAsync(reportsModel.ReportType.Value) ?? new ReportTemplateDefinition();

        if (reportsModel.Template is null)
            return NotFound($"No se encontro template para {reportsModel.ReportType.Value}.");

        var safeSectionIndex = reportsModel.Template.Sections.Count == 0
            ? 0
            : Math.Clamp(reportsModel.SectionIndex, 0, reportsModel.Template.Sections.Count - 1);
        reportsModel.SectionIndex = safeSectionIndex;

        ViewData["SelectedTemplateFile"] = $"{reportsModel.Template.ReportKey}.{reportsModel.Template.Version}.json";

        return View(reportsModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(ReportsTemplateViewModel model)
    {
        if (!model.ReportType.HasValue)
            return BadRequest("Debe especificarse el tipo de reporte.");

        model.Template = await GetTemplateJsonAsync(model.ReportType.Value) ?? new ReportTemplateDefinition();
        if (model.Template is null)
            return NotFound($"No se encontro template para {model.ReportType.Value}.");

        var draft = model.DraftId.HasValue
            ? await db.Reports.FirstOrDefaultAsync(x => x.Id == model.DraftId.Value)
            : null;

        var now = DateTime.UtcNow;
        Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : null;

        if (draft is null)
        {
            draft = new Reports
            {
                Id = Guid.NewGuid(),
                DateCreated = now,
                CreatedBy = userId,
                AnswersJson = "{}"
            };

            db.Reports.Add(draft);
        }

        draft.ReportType = model.ReportType.Value.ToString();
        draft.TemplateKey = model.Template.ReportKey;
        draft.TemplateVersion = model.Template.Version;
        draft.CurrentSectionIndex = model.SectionIndex;
        draft.Status = "draft";
        draft.AnswersJson = MergeAnswers(draft.AnswersJson, ExtractAnswersJson(Request.Form));
        draft.LastModified = now;

        await db.SaveChangesAsync();

        model.SectionIndex++;
        model.DraftId = draft.Id;

        if (model.SectionIndex == model.Template.Sections.Count)
        {
            TempData["ReportDraftSaved"] = "Borrador guardado correctamente.";
            return RedirectToAction(nameof(Index), model);
        }
        return RedirectToAction(nameof(LoadTemplate), model);
    }

    private async Task<ReportTemplateDefinition?> GetTemplateJsonAsync(ReportType reportType)
    {
        if (!TemplateMap.TryGetValue(reportType, out var reportKey))
            return null;

        var templateDirectory = Path.Combine(environment.ContentRootPath, "Views", "Reports", "Templates");
        var matches = Directory.GetFiles(templateDirectory, $"{reportKey}.*.json", SearchOption.TopDirectoryOnly);

        if (matches.Length == 0)
            return null;

        // If more than one version exists, use a deterministic file instead of relying on file system order.
        var filePath = matches
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .First();

        if (!System.IO.File.Exists(filePath))
            return null;

        var rawJson = await System.IO.File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ReportTemplateDefinition>(rawJson, JsonOptions);
    }

    private static string MergeAnswers(string currentAnswersJson, string newAnswersJson)
    {
        var mergedAnswers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(currentAnswersJson))
        {
            var existingAnswers = JsonSerializer.Deserialize<Dictionary<string, string?>>(currentAnswersJson, JsonOptions);
            if (existingAnswers is not null)
            {
                foreach (var pair in existingAnswers)
                {
                    mergedAnswers[pair.Key] = pair.Value;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(newAnswersJson))
        {
            var latestAnswers = JsonSerializer.Deserialize<Dictionary<string, string?>>(newAnswersJson, JsonOptions);
            if (latestAnswers is not null)
            {
                foreach (var pair in latestAnswers)
                {
                    mergedAnswers[pair.Key] = pair.Value;
                }
            }
        }

        return JsonSerializer.Serialize(mergedAnswers, JsonOptions);
    }

    private static string ExtractAnswersJson(IFormCollection form)
    {
        var answers = form
            .Where(pair => pair.Key.StartsWith("answers[", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(answers, JsonOptions);
    }

    private static readonly Dictionary<ReportType, string> TemplateMap = new()
    {
        [ReportType.AnexoTI] = "sep_911_anexo_tic"
    };
}
