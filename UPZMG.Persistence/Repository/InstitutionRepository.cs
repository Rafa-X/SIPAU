using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Institution;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// The institution's master data, read from the official 911.10 capture instead of a table of
/// its own. Keeping a second copy would let the header of a report disagree with the format the
/// university actually sent, which is the very problem this system exists to remove.
/// </summary>
public class InstitutionRepository(AppDBContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The newest capture of the institutional format, preferring a submitted one over a draft.
    /// Returns <see cref="InstitutionProfile.Unreported"/> when the area has not captured it yet.
    /// </summary>
    public async Task<InstitutionProfile> GetProfileAsync()
    {
        var row = await db.Reports
            .AsNoTracking()
            .Where(r => r.ReportType == InstitutionProfile.SourceReportType)
            .Select(r => new { r.Status, r.TemplateVersion, r.LastModified, r.AnswersJson })
            .ToListAsync();

        var source = row
            .OrderByDescending(r => r.Status == nameof(ReportStatus.submitted))
            .ThenByDescending(r => r.TemplateVersion)
            .ThenByDescending(r => r.LastModified)
            .FirstOrDefault();

        if (source is null) return InstitutionProfile.Unreported;

        var answers = Read(source.AnswersJson);

        return new InstitutionProfile(
            InstitutionName: Value(answers, "nombre_institucion") ?? InstitutionProfile.DefaultName,
            InstitutionKey: Value(answers, "clave_institucion"),
            RectorName: Value(answers, "nombre_rector"),
            Sostenimiento: Value(answers, "sostenimiento"),
            Municipality: Value(answers, "municipio"),
            State: Value(answers, "entidad"),
            ResponsibleEmail: Value(answers, "correo_responsable"),
            SourceCycle: source.TemplateVersion,
            SourceStatus: source.Status ?? "draft");
    }

    private static string? Value(Dictionary<string, string> answers, string key) =>
        answers.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static Dictionary<string, string> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
