using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Turns the languages a capture names into catalogue rows. A name the catalogue does not have
/// used to be dropped without a word: the forms offered "Maya" and "Otomí", the catalogue says
/// "Maya Yucateco" and has no Otomí, and the language vanished from the record on saving.
/// </summary>
internal static class LanguageCatalogue
{
    public static async Task<List<Guid>> ResolveAsync(AppDBContext db, IEnumerable<string>? names)
    {
        var requested = (names ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count == 0) return [];

        var catalogue = await db.IndigenousLanguages.AsNoTracking()
            .Select(l => new { l.Id, l.Language })
            .ToListAsync();

        var ids = new List<Guid>();
        var unknown = new List<string>();
        foreach (var name in requested)
        {
            // Accents and case do not decide a match: "Náhuatl" is the catalogue's "Nahuatl".
            var match = catalogue.FirstOrDefault(l => Plain(l.Language) == Plain(name));
            if (match is null) unknown.Add(name);
            else ids.Add(match.Id);
        }

        if (unknown.Count > 0)
        {
            throw new InvalidOperationException(
                $"No se reconoce la lengua {string.Join(", ", unknown.Select(u => $"«{u}»"))}. " +
                $"Las del catálogo son: {string.Join(", ", catalogue.Select(l => l.Language).Order())}.");
        }

        return [.. ids.Distinct()];
    }

    private static string Plain(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        return new string([.. decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)])
            .ToLowerInvariant();
    }
}
