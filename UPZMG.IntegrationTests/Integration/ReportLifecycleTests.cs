using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Reports;
using UPZMG.IntegrationTests.Infrastructure;
using UPZMG.Persistence.Models;
using Xunit;

namespace UPZMG.IntegrationTests.Integration;

/// <summary>
/// An official format from its first draft to its submission: editable while it is a draft,
/// protected against two people overwriting each other, refused while incomplete, and frozen
/// once submitted. Only the person who captured it can read it back.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReportLifecycleTests(TestDatabase database)
{
    private const string Type = "SII_CuerposAcademicos";
    private const string Version = "2024-2025";

    // The answers of the seeded capture of this format, which the engine accepts as complete.
    private static Dictionary<string, string?> Complete(string period) => new()
    {
        ["periodo"] = period,
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

    private static SaveReport Save(ApiSession session, ReportStatus status, Dictionary<string, string?> answers,
        Guid? id = null, string? rowVersion = null) => new()
    {
        RequesterKey = session.UserId,
        ReportId = id,
        ReportType = Type,
        Version = Version,
        Status = status,
        RowVersion = rowVersion,
        Answers = answers,
    };

    [SqlServerFact]
    public async Task ADraftChangesUntilItIsSubmittedAndThenNever()
    {
        await using var api = new ApiHost(database);
        var owner = await api.SignInAsync(Accounts.DireccionAcademica, Accounts.PasswordOf(Accounts.DireccionAcademica));
        var period = await database.ScalarAsync<string>("SELECT TOP 1 SchoolCycle + ' ' + SEP_Period FROM periods ORDER BY StartDate DESC");
        var answers = Complete(period);
        var partial = new Dictionary<string, string?> { ["periodo"] = period };

        // 1. First save of a draft: created.
        var created = await owner.PostAsync("reports/save", Save(owner, ReportStatus.draft, partial));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var draft = await owner.ReadAsync<ReportSaved>(created);
        Assert.NotEqual(Guid.Empty, draft.Id);

        // 2. A draft can be changed, with the version the form read.
        var updated = await owner.PostAsync("reports/save", Save(owner, ReportStatus.draft, partial, draft.Id, draft.RowVersion));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var current = await owner.ReadAsync<ReportSaved>(updated);

        // 3. A save made from a form opened before that change would overwrite it: refused.
        var stale = await owner.PostAsync("reports/save", Save(owner, ReportStatus.draft, partial, draft.Id, draft.RowVersion));
        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);

        //    Leaving the version out is not a way around that check.
        var blind = await owner.PostAsync("reports/save", Save(owner, ReportStatus.draft, partial, draft.Id));
        Assert.Equal(HttpStatusCode.PreconditionFailed, blind.StatusCode);

        // 4. Submitting while required answers are missing is refused, saying which.
        var incomplete = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, partial, draft.Id, current.RowVersion));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
        Assert.Contains("Falta", await incomplete.Content.ReadAsStringAsync());

        // 5. Complete but unbalanced — three research lines against a declared total of four — it is
        //    refused with the reason. The API runs the same engine as the capture screen.
        var unbalanced = new Dictionary<string, string?>(answers) { ["liiadt[#]"] = "3" };
        unbalanced.Remove("liiadt[4][nombre]");
        unbalanced.Remove("liiadt[4][nivel]");
        var refused = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, unbalanced, draft.Id, current.RowVersion));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Contains("deben ser 4", await refused.Content.ReadAsStringAsync());

        // 6. Complete and balanced, it is submitted.
        var submitted = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, answers, draft.Id, current.RowVersion));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var final = await owner.ReadAsync<ReportSaved>(submitted);
        Assert.Equal(ReportStatus.submitted, final.Status);

        // 7. From then on it is an official document: no change is accepted, not even a draft save.
        var edit = await owner.PostAsync("reports/save", Save(owner, ReportStatus.draft, partial, draft.Id, final.RowVersion));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

        // 8. Read back by its author: the answers as submitted. Its id is the folio on the acuse.
        var read = await owner.ReadAsync<ReportDetail>(await owner.Client.GetAsync($"reports/{draft.Id}"));
        Assert.Equal(ReportStatus.submitted, read.Status);
        Assert.Equal(answers.OrderBy(a => a.Key), read.Answers.OrderBy(a => a.Key));

        // 9. Somebody else, even with permission to capture formats, cannot open it.
        var other = await api.SignInAsync(Accounts.ControlEscolar, "User123!");
        Assert.Equal(HttpStatusCode.NotFound, (await other.Client.GetAsync($"reports/{draft.Id}")).StatusCode);
        var theirs = await other.ReadAsync<List<ReportListItem>>(await other.Client.GetAsync("reports/mine"));
        Assert.DoesNotContain(theirs, r => r.Id == draft.Id);
    }

    /// <summary>
    /// The screen only offers what the template lists, and the API took whatever it was sent: an
    /// invented period and level were stored as an official format.
    /// </summary>
    [SqlServerFact]
    public async Task AnAnswerOutsideTheCatalogueIsNotAcceptedAsOfficial()
    {
        await using var api = new ApiHost(database);
        var owner = await api.SignInAsync(Accounts.DireccionAcademica, Accounts.PasswordOf(Accounts.DireccionAcademica));
        var period = await database.ScalarAsync<string>("SELECT TOP 1 SchoolCycle + ' ' + SEP_Period FROM periods ORDER BY StartDate DESC");

        var inventedPeriod = new Dictionary<string, string?>(Complete(period)) { ["periodo"] = "Periodo inventado" };
        var refused = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, inventedPeriod));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Contains("Periodo inventado", await refused.Content.ReadAsStringAsync());

        var inventedLevel = new Dictionary<string, string?>(Complete(period)) { ["liiadt[1][nivel]"] = "Nivel inventado" };
        refused = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, inventedLevel));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Contains("Nivel inventado", await refused.Content.ReadAsStringAsync());

        // A draft may hold it for now: the rule is about what becomes official.
        var draft = await owner.PostAsync("reports/save", Save(owner, ReportStatus.draft, inventedPeriod));
        Assert.Equal(HttpStatusCode.Created, draft.StatusCode);
        await database.ExecuteAsync("DELETE FROM reports WHERE Id = @id", 60,
            new Microsoft.Data.SqlClient.SqlParameter("@id", (await owner.ReadAsync<ReportSaved>(draft)).Id));
    }

    [SqlServerFact]
    public async Task ATableOverItsLimitIsNotAcceptedAsOfficial()
    {
        await using var api = new ApiHost(database);
        var owner = await api.SignInAsync(Accounts.DireccionAcademica, Accounts.PasswordOf(Accounts.DireccionAcademica));
        var period = await database.ScalarAsync<string>("SELECT TOP 1 SchoolCycle + ' ' + SEP_Period FROM periods ORDER BY StartDate DESC");

        var answers = new Dictionary<string, string?>(Complete(period))
        {
            ["liiadt[#]"] = "61",
            // The table has to match the total it declares, or that other rule answers first.
            ["cuerpos_academicos[En formación][Total de LIIADT]"] = "59",
        };
        for (var row = 5; row <= 61; row++)
        {
            answers[$"liiadt[{row}][nombre]"] = $"Línea {row}";
            answers[$"liiadt[{row}][nivel]"] = "En formación";
        }

        var refused = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, answers));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Contains("el máximo son 60", await refused.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// The rule against answers outside a list must not trap what the university already has: a
    /// seeded draft refused for it could never be sent, and nobody could see why.
    /// </summary>
    [SqlServerFact]
    public async Task EverySeededCaptureIsWithinItsLists()
    {
        await using var api = new ApiHost(database);
        var admin = await api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));

        await using var db = database.Context();
        var rows = await db.Reports.AsNoTracking()
            .Where(r => r.ReportType != "InformeJunta")
            .Select(r => new { r.Id, r.ReportType, r.TemplateVersion, r.AnswersJson })
            .ToListAsync();

        // The seed's own ids: other tests of this run may have left captures of their own.
        var captures = rows.Where(r => r.Id.ToString().StartsWith("cccccccc", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(8, captures.Count);

        foreach (var capture in captures)
        {
            var template = await admin.ReadAsync<ReportTemplateDefinition>(
                await admin.Client.GetAsync($"reports/templates/{capture.ReportType}/{capture.TemplateVersion}"));
            var answers = JsonSerializer.Deserialize<Dictionary<string, string?>>(capture.AnswersJson!)!;

            Assert.Empty(new ReportFormState(template, answers).OutOfCatalogue().Select(p => $"{capture.ReportType}: {p.Message}"));
        }
    }

    /// <summary>
    /// Each save replaces the block, so whoever saved second erased the first text without anybody
    /// knowing. Another block is not a conflict, and knowing what is there it can be replaced.
    /// </summary>
    [SqlServerFact]
    public async Task ASectionOfTheBoardReportIsNotOverwrittenWithoutReadingIt()
    {
        await using var api = new ApiHost(database);
        var admin = await api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));

        async Task<List<JsonElement>> BlocksAsync()
        {
            var document = await admin.ReadAsync<JsonElement>(await admin.Client.GetAsync($"boardreport/{Version}"));
            return [.. document.GetProperty("chapters").EnumerateArray()
                .SelectMany(chapter => chapter.GetProperty("blocks").EnumerateArray())
                .Where(block => block.GetProperty("kind").GetString() == "narrative")];
        }

        async Task<JsonElement> SaveAsync(string key, string? text, string? baseText)
        {
            var response = await admin.PostAsync("boardreport/narrative", new { requesterKey = admin.UserId, cycle = Version, blockKey = key, text, baseText });
            return await admin.ReadAsync<JsonElement>(response);
        }

        var blocks = await BlocksAsync();
        var first = blocks[0].GetProperty("blockKey").GetString()!;
        var second = blocks[1].GetProperty("blockKey").GetString()!;

        Assert.True((await SaveAsync(first, "Lo que escribió A", null)).GetProperty("saved").GetBoolean());

        var late = await SaveAsync(first, "Lo que escribió B", null);
        Assert.False(late.GetProperty("saved").GetBoolean());
        Assert.True(late.GetProperty("superseded").GetBoolean());
        Assert.Equal("Lo que escribió A", late.GetProperty("currentText").GetString());
        Assert.Equal("Lo que escribió A", (await BlocksAsync())[0].GetProperty("text").GetString());

        Assert.True((await SaveAsync(second, "Otro apartado", null)).GetProperty("saved").GetBoolean());
        Assert.True((await SaveAsync(first, "Lo que escribió B", "Lo que escribió A")).GetProperty("saved").GetBoolean());
        Assert.Equal("Lo que escribió B", (await BlocksAsync())[0].GetProperty("text").GetString());

        Assert.True((await SaveAsync(first, null, "Lo que escribió B")).GetProperty("saved").GetBoolean());
        Assert.True((await SaveAsync(second, null, "Otro apartado")).GetProperty("saved").GetBoolean());
    }

    /// <summary>The listing used to count rows: "8 de 8" with two formats missing.</summary>
    [SqlServerFact]
    public async Task SendingAFormatAgainDoesNotAdvanceTheBoardReport()
    {
        await using var api = new ApiHost(database);
        var owner = await api.SignInAsync(Accounts.DireccionAcademica, Accounts.PasswordOf(Accounts.DireccionAcademica));
        var admin = await api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
        var period = await database.ScalarAsync<string>("SELECT TOP 1 SchoolCycle + ' ' + SEP_Period FROM periods ORDER BY StartDate DESC");

        async Task<int> SentAsync()
        {
            var cycles = await admin.ReadAsync<List<UPZMG.Implementations.BoardReport.BoardReportListItem>>(await admin.Client.GetAsync("boardreport/cycles"));
            return cycles.Single(c => c.Cycle == Version).SubmittedFormats;
        }

        var ids = new List<Guid>();
        var submitted = await SentAsync();

        try
        {
            for (var copy = 0; copy < 2; copy++)
            {
                var again = await owner.PostAsync("reports/save", Save(owner, ReportStatus.submitted, Complete(period)));
                Assert.Equal(HttpStatusCode.Created, again.StatusCode);
                ids.Add((await owner.ReadAsync<ReportSaved>(again)).Id);
            }

            Assert.Equal(Math.Max(submitted, 1), await SentAsync());
        }
        finally
        {
            foreach (var id in ids)
                await database.ExecuteAsync("DELETE FROM reports WHERE Id = @id", 60, new Microsoft.Data.SqlClient.SqlParameter("@id", id));
        }
    }
}
