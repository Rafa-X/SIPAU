using System.Net;
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
}
