using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Reports;
using UPZMG.Persistence;
using UPZMG.Persistence.Repository;
using Xunit;

namespace UPZMG.Tests.Reports;

/// <summary>
/// The official formats exactly as the API publishes them, read the way the API reads them, and
/// the engine that totals their cells and refuses a capture whose sums do not add up.
/// </summary>
public class TemplateEngineTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "ReportTemplates");

    private static ReportTemplatesRepository Repository() =>
        new(new AppDBContext(new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase("templates-" + Guid.NewGuid())
                .Options),
            new ReportTemplateStore(Folder));

    // "SII_CuerposAcademicos_2024-2025" → report type and cycle.
    private static (string Type, string Version) Split(string file) =>
        (file[..file.LastIndexOf('_')], file[(file.LastIndexOf('_') + 1)..]);

    private static IEnumerable<string> Files() =>
        Directory.GetFiles(Folder, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)).Order();

    public static TheoryData<string> Published => [.. Files()];

    private static async Task<ReportTemplateDefinition> LoadAsync(string type, string version = "2024-2025") =>
        await Repository().GetTemplateAsync(type, version) ?? throw new InvalidOperationException($"No template {type} {version}.");

    // The capture of «SII · Cuerpos académicos» that seed-report-captures.sql submits.
    private static Dictionary<string, string?> AcademicBodies() => new()
    {
        ["periodo"] = "2026 Sep-Dic",
        ["cuerpos_academicos[En formación][Cuántos]"] = "3",
        ["cuerpos_academicos[En formación][Total de LIIADT]"] = "2",
        ["cuerpos_academicos[En formación][Integrantes mujeres]"] = "4",
        ["cuerpos_academicos[En formación][Integrantes hombres]"] = "3",
        ["cuerpos_academicos[En consolidación][Cuántos]"] = "0",
        ["cuerpos_academicos[En consolidación][Total de LIIADT]"] = "0",
        ["cuerpos_academicos[En consolidación][Integrantes mujeres]"] = "3",
        ["cuerpos_academicos[En consolidación][Integrantes hombres]"] = "15",
        ["cuerpos_academicos[Consolidado][Cuántos]"] = "1",
        ["cuerpos_academicos[Consolidado][Total de LIIADT]"] = "2",
        ["cuerpos_academicos[Consolidado][Integrantes mujeres]"] = "7",
        ["cuerpos_academicos[Consolidado][Integrantes hombres]"] = "4",
        ["liiadt[#]"] = "4",
        ["liiadt[1][nombre]"] = "Línea 1",
        ["liiadt[1][nivel]"] = "En formación",
        ["liiadt[2][nombre]"] = "Línea 2",
        ["liiadt[2][nivel]"] = "En consolidación",
        ["liiadt[3][nombre]"] = "Línea 3",
        ["liiadt[3][nivel]"] = "Consolidado",
        ["liiadt[4][nombre]"] = "Línea 4",
        ["liiadt[4][nivel]"] = "En formación",
    };

    private static ReportTemplateQuestion Question(ReportTemplateDefinition template, string key) =>
        template.Sections.SelectMany(s => s.Questions).Single(q => q.Key == key);

    [Theory]
    [MemberData(nameof(Published))]
    public async Task EveryPublishedFormatLoadsAndPassesTheValidator(string file)
    {
        var (type, version) = Split(file);

        var template = await LoadAsync(type, version);
        var validation = TemplateValidator.Validate(template);

        Assert.True(validation.IsValid, string.Join("\n", validation.Problems.Select(p => $"{p.Where}: {p.Message}")));
    }

    [Fact]
    public async Task TheCatalogueHasEightFormatsWith52SectionsAnd175Questions()
    {
        var templates = await Task.WhenAll(Files().Select(file =>
        {
            var (type, version) = Split(file);
            return LoadAsync(type, version);
        }));

        Assert.Equal(8, templates.Length);
        Assert.Equal(52, templates.Sum(t => t.Sections.Count));
        Assert.Equal(175, templates.Sum(t => t.Sections.Sum(s => s.Questions.Count)));
    }

    [Fact]
    public async Task TheEngineTotalsTheCellsOfAMatrix()
    {
        var template = await LoadAsync("SII_CuerposAcademicos");
        var state = new ReportFormState(template, AcademicBodies());
        var matrix = Question(template, "cuerpos_academicos");

        Assert.Equal(4, state.ColumnTotal(matrix, "Cuántos"));          // 3 + 0 + 1
        Assert.Equal(4, state.ColumnTotal(matrix, "Total de LIIADT"));  // 2 + 0 + 2
        Assert.Equal(11, state.RowTotal(matrix, "Consolidado"));          // 7 women + 4 men
        Assert.Equal(36, state.RowTotalsSum(matrix));                     // every member
        Assert.True(state.IsComplete);
    }

    [Fact]
    public async Task ACaptureWhoseSumsDoNotMatchIsRefusedWithTheReason()
    {
        var template = await LoadAsync("SII_CuerposAcademicos");
        var answers = AcademicBodies();

        // Three research lines registered against a declared total of four.
        answers["liiadt[#]"] = "3";
        answers.Remove("liiadt[4][nombre]");
        answers.Remove("liiadt[4][nivel]");

        var state = new ReportFormState(template, answers);

        Assert.False(state.IsComplete);
        var violation = Assert.Single(state.Violations);
        Assert.Equal("liiadt", violation.QuestionKey);
        Assert.Contains("deben ser 4", violation.Message);
    }

    [Fact]
    public async Task AMissingRequiredAnswerKeepsTheCaptureFromBeingSubmitted()
    {
        var template = await LoadAsync("SII_CuerposAcademicos");
        var answers = AcademicBodies();
        answers.Remove("periodo");

        var state = new ReportFormState(template, answers);

        Assert.False(state.IsComplete);
        Assert.Contains(state.Missing, m => m.QuestionKey == "periodo");
        Assert.Equal(state.TotalRequired - 1, state.AnsweredRequired);
    }
}
