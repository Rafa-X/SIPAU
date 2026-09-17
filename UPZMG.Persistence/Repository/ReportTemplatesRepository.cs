using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Reports;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>Folder with the official format templates, one "{reportType}_{version}.json" per format.</summary>
public sealed record ReportTemplateStore(string Folder);

public class ReportTemplatesRepository(AppDBContext db, ReportTemplateStore store)
{
    // Far above the largest official format; stops a runaway paste.
    private const int MaxTemplateChars = 512 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    // Every template on disk, broken ones included, so whoever maintains them can see what to fix
    public async Task<List<ReportTemplateItem>> GetTemplatesAsync()
    {
        if (!Directory.Exists(store.Folder)) return [];

        var items = new List<ReportTemplateItem>();
        foreach (var path in Directory.GetFiles(store.Folder, "*.json").Order(StringComparer.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var (template, error) = await ReadAsync(path);

            if (template is null)
            {
                items.Add(new ReportTemplateItem(fileName, string.Empty, fileName, 0, 0, [error]));
                continue;
            }

            var problems = TemplateValidator.Validate(template).Problems.Select(p => p.ToString()).ToList();

            // The file name is how a template is found again, so it must match what the JSON declares.
            var expected = $"{template.ReportType}_{template.Version}";
            if (!fileName.Equals(expected, StringComparison.OrdinalIgnoreCase))
                problems.Add($"Plantilla: el archivo debe llamarse «{expected}.json».");

            items.Add(new ReportTemplateItem(
                template.ReportType,
                template.Version,
                template.DisplayName,
                template.Sections.Count,
                template.Sections.Sum(s => s.Questions.Count),
                problems));
        }

        return items.OrderBy(t => t.ReportType).ThenByDescending(t => t.Version).ToList();
    }

    // One capturable template with its catalogue rows and options filled in; null when missing or broken
    public async Task<ReportTemplateDefinition?> GetTemplateAsync(string reportType, string version)
    {
        var path = PathFor(reportType, version);
        if (path is null || !File.Exists(path)) return null;

        var (template, _) = await ReadAsync(path);
        if (template is null || !TemplateValidator.Validate(template).IsValid) return null;

        if (!template.ReportType.Equals(reportType, StringComparison.OrdinalIgnoreCase)
            || !template.Version.Equals(version, StringComparison.OrdinalIgnoreCase)) return null;

        await ResolveCatalogsAsync(template);
        return template;
    }

    // The file as it was authored, which is what the template editor edits
    public async Task<string?> GetSourceAsync(string reportType, string version)
    {
        var path = PathFor(reportType, version);
        return path is not null && File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
    }

    /// <summary>
    /// Writes a template once it parses and validates. Overwriting a different
    /// format is refused: replacing one an área already captured against is a
    /// mistake, not an update.
    /// </summary>
    public async Task<TemplateSaveResult> SaveTemplateAsync(SaveReportTemplate request)
    {
        if (request.Json.Length > MaxTemplateChars)
            return TemplateSaveResult.Refused("Plantilla: el archivo es demasiado grande.");

        var replacing = string.IsNullOrWhiteSpace(request.ReplacingFile) ? null : request.ReplacingFile.Trim();
        if (replacing is not null && !TemplateValidator.IsSafeName(replacing, 80))
            return TemplateSaveResult.Refused("Plantilla: el archivo que se reemplaza no tiene un nombre válido.");

        ReportTemplateDefinition? template;
        try
        {
            template = JsonSerializer.Deserialize<ReportTemplateDefinition>(request.Json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return TemplateSaveResult.Refused($"Plantilla: JSON inválido cerca de la línea {(ex.LineNumber ?? 0) + 1}.");
        }

        if (template is null) return TemplateSaveResult.Refused("Plantilla: el contenido está vacío.");

        var validation = TemplateValidator.Validate(template);
        if (!validation.IsValid)
            return new TemplateSaveResult(false, null, [.. validation.Problems.Select(p => p.ToString())]);

        var fileKey = $"{template.ReportType}_{template.Version}";
        var path = Path.Combine(store.Folder, fileKey + ".json");

        if (File.Exists(path) && !fileKey.Equals(replacing, StringComparison.OrdinalIgnoreCase))
            return TemplateSaveResult.Refused(
                $"Plantilla: ya existe «{template.ReportType}» versión {template.Version}. Cambia la clave o la versión.");

        Directory.CreateDirectory(store.Folder);
        // Stored as it was written: the file stays readable in a diff, and any
        // comments explaining the official format survive.
        await File.WriteAllTextAsync(path, request.Json.Trim() + Environment.NewLine);

        // A renamed template would otherwise be offered twice, from both files.
        if (replacing is not null && !fileKey.Equals(replacing, StringComparison.OrdinalIgnoreCase))
        {
            var previous = Path.Combine(store.Folder, replacing + ".json");
            if (File.Exists(previous)) File.Delete(previous);
        }

        return new TemplateSaveResult(true, fileKey);
    }

    /// <summary>Removes a template, unless somebody already captured with it.</summary>
    public async Task<TemplateSaveResult> DeleteTemplateAsync(DeleteReportTemplate request)
    {
        var path = PathFor(request.ReportType, request.Version);
        if (path is null || !File.Exists(path)) return TemplateSaveResult.Refused("Plantilla: no existe.");

        var captures = await db.Reports
            .CountAsync(r => r.ReportType == request.ReportType && r.TemplateVersion == request.Version);

        if (captures > 0)
            return TemplateSaveResult.Refused(
                $"Plantilla: hay {captures} captura(s) con este formato, así que no se puede eliminar.");

        File.Delete(path);
        return new TemplateSaveResult(true, Path.GetFileNameWithoutExtension(path));
    }

    // Report type and version reach here from the URL: only plain names may touch the file system.
    private string? PathFor(string reportType, string version) =>
        TemplateValidator.IsSafeReportType(reportType) && TemplateValidator.IsSafeVersion(version)
            ? Path.Combine(store.Folder, $"{reportType}_{version}.json")
            : null;

    /// <summary>
    /// Replaces optionsFrom / rowsFrom with the catalogue values, so each SEP-dictated
    /// list lives once, in the database. An empty catalogue leaves the list empty and
    /// the capture screen explains that the catalogue still has to be loaded.
    /// </summary>
    private async Task ResolveCatalogsAsync(ReportTemplateDefinition template)
    {
        var cache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var question in template.Sections.SelectMany(s => s.Questions))
        {
            if (!string.IsNullOrWhiteSpace(question.OptionsFrom) && question.Options.Count == 0)
                question.Options = [.. await CatalogAsync(question.OptionsFrom.Trim(), cache)];

            if (!string.IsNullOrWhiteSpace(question.RowsFrom) && question.Rows.Count == 0)
                question.Rows = [.. await CatalogAsync(question.RowsFrom.Trim(), cache)];
        }
    }

    // Seeded catalogues use sequential ids, and SQL Server sorts them in that sequence,
    // so ordering by id keeps the order of the official list.
    private async Task<List<string>> CatalogAsync(string name, Dictionary<string, List<string>> cache)
    {
        if (cache.TryGetValue(name, out var cached)) return cached;

        List<string> values = name.ToLowerInvariant() switch
        {
            ReportCatalogs.Disabilities => await db.Set<Disabilities>()
                .AsNoTracking()
                .Where(d => d.Name != null)
                .OrderBy(d => d.Id)
                .Select(d => d.Name!)
                .ToListAsync(),

            ReportCatalogs.WithdrawReasons => await db.Set<WithdrawReason>()
                .AsNoTracking()
                .OrderBy(w => w.Id)
                .Select(w => w.Description)
                .ToListAsync(),

            ReportCatalogs.Scholarships => await db.Set<ScolarshipProgram>()
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Select(s => s.OfficialName)
                .ToListAsync(),

            ReportCatalogs.IndigenousLanguages => await db.IndigenousLanguages
                .AsNoTracking()
                .OrderBy(l => l.Language)
                .Select(l => l.Language)
                .ToListAsync(),

            ReportCatalogs.Periods => await db.Periods
                .AsNoTracking()
                .OrderByDescending(p => p.StartDate)
                .Select(p => p.SchoolCycle + " " + p.SEP_Period)
                .ToListAsync(),

            _ => [],
        };

        cache[name] = values;
        return values;
    }

    private static async Task<(ReportTemplateDefinition? Template, string Error)> ReadAsync(string path)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var template = await JsonSerializer.DeserializeAsync<ReportTemplateDefinition>(stream, JsonOptions);
            return template is null ? (null, "Plantilla: el archivo está vacío.") : (template, string.Empty);
        }
        catch (JsonException ex)
        {
            return (null, $"Plantilla: JSON inválido cerca de la línea {(ex.LineNumber ?? 0) + 1}.");
        }
        catch (IOException ex)
        {
            return (null, $"Plantilla: no se pudo leer el archivo ({ex.Message}).");
        }
    }
}
