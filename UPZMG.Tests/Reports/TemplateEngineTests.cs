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

    private static ReportTemplateDefinition OneQuestion(ReportTemplateQuestion question) => new()
    {
        ReportType = "Prueba",
        DisplayName = "Prueba",
        Version = "2024-2025",
        Sections = [new ReportTemplateSection { Key = "s", Title = "Sección", Questions = [question] }],
    };

    private static ReportFormState Answered(ReportTemplateQuestion question, string key, string value) =>
        new(OneQuestion(question), new Dictionary<string, string?> { [key] = value });

    [Fact]
    public async Task AnAnswerOutsideATableListIsNotOfficial()
    {
        var answers = AcademicBodies();
        answers["liiadt[2][nivel]"] = "Nivel inventado";

        var form = new ReportFormState(await LoadAsync("SII_CuerposAcademicos"), answers);

        var problem = Assert.Single(form.OutOfCatalogue());
        Assert.Contains("Línea 2", problem.Message);
        Assert.Contains("Nivel inventado", problem.Message);
    }

    [Fact]
    public async Task ATableOverItsLimitIsNotOfficial()
    {
        var answers = AcademicBodies();
        answers["liiadt[#]"] = "61";

        var form = new ReportFormState(await LoadAsync("SII_CuerposAcademicos"), answers);

        Assert.Contains(form.OutOfCatalogue(), problem => problem.Message.Contains("el máximo son 60"));
    }

    [Fact]
    public async Task TheSeededCaptureIsWithinItsLists() =>
        Assert.Empty(new ReportFormState(await LoadAsync("SII_CuerposAcademicos"), AcademicBodies()).OutOfCatalogue());

    [Theory]
    [InlineData("select", "q", "C", true)]
    [InlineData("select", "q", "a", false)]
    [InlineData("matrix_select", "q[Fila]", "C", true)]
    [InlineData("matrix_select", "q[Fila]", "B", false)]
    [InlineData("boolean", "q", "quizá", true)]
    [InlineData("boolean", "q", "true", false)]
    [InlineData("date", "q", "ayer", true)]
    [InlineData("date", "q", "2026-11-02", false)]
    public void AnAnswerTheScreenCouldNotHaveGivenIsFound(string type, string key, string value, bool found)
    {
        var question = new ReportTemplateQuestion
        {
            Key = "q", Text = "Pregunta", Type = type, Options = ["A", "B"], Rows = ["Fila"],
        };

        Assert.Equal(found, Answered(question, key, value).OutOfCatalogue().Count > 0);
    }

    [Fact]
    public void ACheckedOptionOutsideTheListIsFound()
    {
        var question = new ReportTemplateQuestion { Key = "q", Text = "Pregunta", Type = "checkbox_group", Options = ["A", "B"] };
        var form = new ReportFormState(OneQuestion(question));

        form.ToggleOption("q", "A", true);
        Assert.Empty(form.OutOfCatalogue());

        form.ToggleOption("q", "Inventada", true);
        Assert.Contains("Inventada", Assert.Single(form.OutOfCatalogue()).Message);
    }

    [Fact]
    public void OptionsWrittenWithSemicolonsAreReadAsSeveralOptions()
    {
        var question = new ReportTemplateQuestion { Key = "q", Text = "Pregunta", Type = "checkbox_group", Options = ["A", "B", "C"] };

        var form = Answered(question, "q", "A;B");

        // As the seed writes them: each box shows ticked, and each can be unticked.
        Assert.True(form.IsChecked("q", "A"));
        Assert.True(form.IsChecked("q", "B"));
        Assert.Equal("A, B", form.CheckedSummary("q"));
        Assert.Empty(form.OutOfCatalogue());

        form.ToggleOption("q", "A", false);
        Assert.Equal("B", form.CheckedSummary("q"));
    }

    [Fact]
    public void AnOptionThatContainsASemicolonStaysWhole()
    {
        var question = new ReportTemplateQuestion { Key = "q", Text = "Pregunta", Type = "checkbox_group", Options = ["Uno; dos", "Tres"] };

        var form = Answered(question, "q", "Uno; dos");

        Assert.True(form.IsChecked("q", "Uno; dos"));
        Assert.Empty(form.OutOfCatalogue());
    }

    [Fact]
    public void ACellOfATableKeepsToItsType()
    {
        var question = new ReportTemplateQuestion
        {
            Key = "t", Text = "Tabla", Type = "table",
            Fields =
            [
                new ReportTemplateField { Key = "n", Text = "Cantidad", Type = "number" },
                new ReportTemplateField { Key = "d", Text = "Fecha", Type = "date" },
            ],
        };
        var form = new ReportFormState(OneQuestion(question), new Dictionary<string, string?>
        {
            ["t[#]"] = "1",
            ["t[1][n]"] = "muchos",
            ["t[1][d]"] = "ayer",
        });

        Assert.Equal(2, form.OutOfCatalogue().Count);
    }

    [Fact]
    public void AHiddenQuestionIsNotChecked()
    {
        var question = new ReportTemplateQuestion { Key = "q", Text = "Pregunta", Type = "select", Options = ["A"], ShowIf = "otra" };

        Assert.Empty(Answered(question, "q", "Inventada").OutOfCatalogue());
    }

    /// <summary>
    /// A format that was already sent is printed with its violations. A catalogue that changes
    /// later must not make that document look wrong, so this stays out of them.
    /// </summary>
    [Fact]
    public void AnOptionOutsideTheListIsNotARuleBrokenOnPaper()
    {
        var question = new ReportTemplateQuestion { Key = "q", Text = "Pregunta", Type = "select", Options = ["A"] };

        var form = Answered(question, "q", "Periodo que ya no existe");

        Assert.Empty(form.Violations);
        Assert.NotEmpty(form.OutOfCatalogue());
    }

    private static List<XlsxCell> Cells(ReportFormState form) =>
        [.. ReportExport.ToSheets(form).SelectMany(sheet => sheet.Rows).SelectMany(row => row)];

    [Theory]
    [InlineData("00123")]
    [InlineData("1E5")]
    public void AnIdentifierKeepsItsTextInTheWorkbook(string identifier)
    {
        var question = new ReportTemplateQuestion { Key = "clave", Text = "Clave", Type = "text" };

        var cell = Assert.Single(Cells(Answered(question, "clave", identifier)), c => c.Text == identifier);

        Assert.Null(cell.Number);
    }

    [Fact]
    public void ATextCellOfATableKeepsItsZerosAndANumberCellAddsUp()
    {
        var question = new ReportTemplateQuestion
        {
            Key = "t", Text = "Tabla", Type = "table",
            Fields =
            [
                new ReportTemplateField { Key = "clave", Text = "Clave", Type = "text" },
                new ReportTemplateField { Key = "n", Text = "Cantidad", Type = "number" },
            ],
        };
        var form = new ReportFormState(OneQuestion(question), new Dictionary<string, string?>
        {
            ["t[#]"] = "1",
            ["t[1][clave]"] = "00123",
            ["t[1][n]"] = "0456",
        });

        var cells = Cells(form);

        Assert.Contains(cells, c => c.Text == "00123");
        Assert.Contains(cells, c => c.Number == 456);
    }

    [Fact]
    public void TheAnswerToANumberQuestionIsANumberInTheWorkbook()
    {
        var question = new ReportTemplateQuestion { Key = "n", Text = "Cuántos", Type = "number" };

        Assert.Contains(Cells(Answered(question, "n", "123")), c => c.Number == 123);
    }

    [Theory]
    [MemberData(nameof(Published))]
    public async Task EveryPublishedFormatLoadsAndPassesTheValidator(string file)
    {
        var (type, version) = Split(file);

        var template = await LoadAsync(type, version);
        var validation = TemplateValidator.Validate(template);

        Assert.True(validation.IsValid, string.Join("\n", validation.Problems.Select(p => $"{p.Where}: {p.Message}")));
    }

    // JSON allows a null almost anywhere, and it used to reach the validator as a 500.
    [Theory]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": "x", "sections": null }""")]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": "x", "sections": [null] }""")]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": "x", "sections": [{ "title": "S", "questions": null }] }""")]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": "x", "sections": [{ "title": "S", "questions": [null] }] }""")]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": "x", "sections": [{ "title": "S", "questions": [{ "key": "m", "text": "m", "type": "matrix", "rows": null, "columns": ["a"] }] }] }""")]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": "x", "sections": [{ "title": "S", "questions": [{ "key": "m", "text": "m", "type": "matrix", "rows": [null], "columns": ["a"] }] }] }""")]
    [InlineData("""{ "reportType": "X", "version": "1", "displayName": null, "sections": [] }""")]
    public async Task ATemplateWithNullsIsRefusedWithAReason(string json)
    {
        var folder = Directory.CreateTempSubdirectory("plantillas-").FullName;
        try
        {
            var repository = new ReportTemplatesRepository(
                new AppDBContext(new DbContextOptionsBuilder<AppDBContext>().UseInMemoryDatabase("templates-" + Guid.NewGuid()).Options),
                new ReportTemplateStore(folder));

            var result = await repository.SaveTemplateAsync(new SaveReportTemplate { RequesterKey = Guid.NewGuid(), Json = json });

            Assert.False(result.Saved);
            Assert.NotEmpty(result.Problems!);
            Assert.Empty(Directory.GetFiles(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task AFileEditedByHandWithNullsDoesNotTakeTheListDown()
    {
        var folder = Directory.CreateTempSubdirectory("plantillas-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "Mala_1.json"),
                """{ "reportType": "Mala", "version": "1", "displayName": "x", "sections": [{ "title": "S", "questions": [{ "key": "m", "text": "m", "type": "matrix", "rows": null }] }] }""");
            await File.WriteAllTextAsync(Path.Combine(folder, "Vacia_1.json"),
                """{ "reportType": "Vacia", "version": "1", "displayName": "x", "sections": [null] }""");
            File.Copy(Path.Combine(Folder, Files().First() + ".json"), Path.Combine(folder, Files().First() + ".json"));

            var repository = new ReportTemplatesRepository(
                new AppDBContext(new DbContextOptionsBuilder<AppDBContext>().UseInMemoryDatabase("templates-" + Guid.NewGuid()).Options),
                new ReportTemplateStore(folder));

            var listed = await repository.GetTemplatesAsync();

            Assert.Equal(3, listed.Count);
            Assert.Equal(2, listed.Count(t => t.Problems.Count > 0));
            Assert.Null(await repository.GetTemplateAsync("Mala", "1"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
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
