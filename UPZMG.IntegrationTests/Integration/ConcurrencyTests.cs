using System.Net;
using System.Text.Json;
using UPZMG.IntegrationTests.Infrastructure;
using Xunit;

namespace UPZMG.IntegrationTests.Integration;

/// <summary>
/// Many requests at once against one API and one database, as on the day every area captures:
/// two people saving the same student, everybody opening the risk screen at the start of a
/// period, and retrainings requested while others read the model.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ConcurrencyTests(TestDatabase database)
{
    [SqlServerFact]
    public async Task TwelveSimultaneousCapturesOfOneStudentStoreItOnce()
    {
        await using var api = new ApiHost(database);
        var sessions = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(i => api.SignInAsync(i % 2 == 0 ? Accounts.ControlEscolar : Accounts.ServiciosEstudiantiles, "User123!")));
        var student = await Samples.StudentAsync(database, "IT-CONC-0001", "CONC000101HJCRRS01");

        var responses = await Task.WhenAll(sessions.Select(s => s.PostAsync("data-capture/students/create", student)));

        // One is created; every other one is told it already exists. None fails with a 500.
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(11, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, await Samples.StudentsWithAsync(database, "IT-CONC-0001"));
    }

    [SqlServerFact]
    public async Task ColdRequestsForTheModelShareOneTraining()
    {
        // A host of its own, so its model cache starts empty.
        await using var api = new ApiHost(database);
        var sessions = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin))));

        var reports = await Task.WhenAll(sessions.Select(async s =>
        {
            var response = await s.Client.GetAsync("intelligence/dropout");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await s.ReadAsync<JsonElement>(response);
        }));

        // Eight readers, one training: the same run and the same training time for all of them.
        Assert.Single(reports.Select(r => r.GetProperty("runId").GetString()).Distinct());
        Assert.Single(reports.Select(r => r.GetProperty("trainedAtUtc").GetDateTime()).Distinct());
        Assert.True(reports[0].GetProperty("hasModel").GetBoolean(), reports[0].GetProperty("status").GetString());
    }

    [SqlServerFact]
    public async Task RetrainingsWhileOthersReadLeaveOneConsistentModel()
    {
        await using var api = new ApiHost(database);
        var admin = await api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
        var readers = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => api.SignInAsync(Accounts.ControlEscolar, "User123!")));

        var work = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 4; i++)
        {
            work.Add(admin.PostAsync("intelligence/dropout/retrain", new { requesterKey = admin.UserId }));
            work.Add(readers[i].Client.GetAsync("intelligence/dropout/students"));
            work.Add(readers[i].Client.GetAsync("intelligence/dropout"));
        }

        var responses = await Task.WhenAll(work);
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        // Afterwards the summary and the student list come from the same training.
        var report = await admin.ReadAsync<JsonElement>(await admin.Client.GetAsync("intelligence/dropout"));
        var list = await admin.ReadAsync<JsonElement>(await admin.Client.GetAsync("intelligence/dropout/students"));
        Assert.Equal(report.GetProperty("runId").GetString(), list.GetProperty("runId").GetString());
        Assert.Equal(report.GetProperty("scoredStudents").GetInt32(), list.GetProperty("students").GetArrayLength());
    }
}
