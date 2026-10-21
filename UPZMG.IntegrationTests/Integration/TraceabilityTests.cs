using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using UPZMG.Implementations.Reports;
using UPZMG.IntegrationTests.Infrastructure;
using Xunit;

namespace UPZMG.IntegrationTests.Integration;

/// <summary>
/// From capture to the Junta report without typing anything twice: each figure the report takes
/// from an official format is that format's submitted capture and names it, a format still in draft
/// contributes nothing, and the report counts the same students as the dashboard.
/// The captures are the ones seed-report-captures.sql loads: six submitted, two drafts.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TraceabilityTests(TestDatabase database) : IAsyncLifetime
{
    private const string Cycle = "2024-2025";
    private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");

    private ApiHost _api = null!;
    private ApiSession _admin = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiHost(database);
        _admin = await _api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private async Task<List<JsonElement>> BlocksAsync()
    {
        var response = await _admin.Client.GetAsync($"boardreport/{Cycle}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await _admin.ReadAsync<JsonElement>(response);
        return [.. document.GetProperty("chapters").EnumerateArray().SelectMany(c => c.GetProperty("blocks").EnumerateArray())];
    }

    private static string Text(JsonElement block, string property) =>
        block.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : string.Empty;

    private static double Number(JsonElement value) => double.Parse(value.GetString()!, NumberStyles.Number, Mx);

    [SqlServerFact]
    public async Task EachFigureTakenFromAFormatIsItsSubmittedCaptureAndNamesIt()
    {
        var block = (await BlocksAsync()).Single(b => Text(b, "title") == "Matrícula reportada por grado");

        // It says which capture it read: the format, when it was submitted and by whom.
        var source = Text(block, "source");
        Assert.Contains("Formato 911.9A", source);
        Assert.Contains("enviado el", source);
        Assert.Contains(Accounts.ControlEscolar, source);

        // Its figures are that capture's answers as the format engine reads them, cell by cell.
        var template = await _admin.ReadAsync<ReportTemplateDefinition>(
            await _admin.Client.GetAsync($"reports/templates/Carrera911_9A/{Cycle}"));
        var answers = JsonSerializer.Deserialize<Dictionary<string, string?>>(await database.ScalarAsync<string>(
            "SELECT AnswersJson FROM reports WHERE ReportType = N'Carrera911_9A' AND TemplateVersion = @cycle AND Status = N'submitted'",
            new SqlParameter("@cycle", Cycle)))!;
        var state = new ReportFormState(template, answers);
        var matrix = template.Sections.SelectMany(s => s.Questions).Single(q => q.Key == "matricula_grado");

        var rows = block.GetProperty("table").GetProperty("rows").EnumerateArray().ToList();
        Assert.NotEmpty(rows);
        foreach (var row in rows)
        {
            var label = row.GetProperty("label").GetString()!;
            var values = row.GetProperty("values").EnumerateArray().ToList();
            for (int c = 0; c < matrix.Columns.Count; c++)
                Assert.Equal(state.CellTotal(matrix, label, matrix.Columns[c]), Number(values[c]));
        }
    }

    [SqlServerFact]
    public async Task AFormatStillInDraftContributesNoFigures()
    {
        // The TI annex is a draft in the seed: its block says so instead of showing its numbers.
        var pending = (await BlocksAsync())
            .Where(b => Text(b, "kind") == "pending")
            .Select(b => Text(b, "text"))
            .ToList();

        Assert.Contains(pending, text => text.Contains("Tecnologías de la Información") && text.Contains("sigue en borrador"));
    }

    [SqlServerFact]
    public async Task TheReportAndTheDashboardCountTheSameStudents()
    {
        var summary = await _admin.ReadAsync<JsonElement>(await _admin.Client.GetAsync("dashboard/summary"));
        var active = summary.GetProperty("activeStudents").GetInt32();

        var block = (await BlocksAsync()).Single(b => Text(b, "title") == "Alumnos activos por programa educativo");
        var total = Number(block.GetProperty("table").GetProperty("total")[0]);

        Assert.Equal(active, total);
        Assert.Equal(active, await database.ScalarAsync<int>("SELECT COUNT(*) FROM students WHERE Status = 0"));
    }
}
