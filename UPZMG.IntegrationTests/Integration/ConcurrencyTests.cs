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

    /// <summary>
    /// The guard reads who else can administer and then writes. Two administrators suspending each
    /// other at the same time both saw the other still active, and it left nobody, every time.
    /// </summary>
    [SqlServerFact]
    public async Task TwoAdministratorsSuspendingEachOtherLeaveOneStanding()
    {
        await using var api = new ApiHost(database);
        var first = await api.SignInAsync(Accounts.Admin, Accounts.PasswordOf(Accounts.Admin));
        var email = await database.ScalarAsync<string>(
            "SELECT TOP 1 e.Email FROM employees e WHERE NOT EXISTS (SELECT 1 FROM systemUser u WHERE u.Email = e.Email) ORDER BY e.Email");
        var adminRole = await database.ScalarAsync<Guid>("SELECT Id FROM roles WHERE Name = 'Admin'");

        var created = await first.PostAsync("users/create", new
        {
            requesterKey = first.UserId, userName = "Segundo administrador", email, roleId = adminRole.ToString(),
            isActive = true, password = "SegundoAdmin1",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var second = await api.SignInAsync(email, "SegundoAdmin1");

        const string ActiveAdministrators = "SELECT COUNT(*) FROM systemUser u JOIN userRoles ur ON ur.UserId = u.Id JOIN roles r ON r.Id = ur.RoleId WHERE u.IsActive = 1 AND r.Name = 'Admin'";

        try
        {
            for (var round = 0; round < 5; round++)
            {
                var both = await Task.WhenAll(
                    first.PostAsync("users/set-active", new { requesterKey = first.UserId, userId = second.UserId, isActive = false }),
                    second.PostAsync("users/set-active", new { requesterKey = second.UserId, userId = first.UserId, isActive = false }));

                Assert.Single(both, r => r.StatusCode == HttpStatusCode.OK);
                Assert.Equal(1, await database.ScalarAsync<int>(ActiveAdministrators));

                await database.ExecuteAsync($"UPDATE systemUser SET IsActive = 1 WHERE Id IN ('{first.UserId}', '{second.UserId}')");
            }
        }
        finally
        {
            await database.ExecuteAsync($"UPDATE systemUser SET IsActive = 1 WHERE Id = '{first.UserId}'");
            await database.ExecuteAsync($"DELETE FROM userRoles WHERE UserId = '{second.UserId}'");
            await database.ExecuteAsync($"DELETE FROM systemUser WHERE Id = '{second.UserId}'");
        }
    }

    /// <summary>
    /// No index can hold "this room is free at this hour", so twelve requests for one slot all read
    /// it free and several went through.
    /// </summary>
    [SqlServerFact]
    public async Task TwelveRequestsForOneSlotCreateOneGroup()
    {
        await using var api = new ApiHost(database);
        var sessions = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => api.SignInAsync(Accounts.DireccionAcademica, Accounts.PasswordOf(Accounts.DireccionAcademica))));

        var career = await database.ScalarAsync<Guid>("SELECT TOP 1 CareerId FROM courses ORDER BY Id");
        var request = new
        {
            requesterKey = Guid.Empty,
            id = Guid.Empty,
            periodId = await database.ScalarAsync<Guid>("SELECT Id FROM periods WHERE IsActive = 1"),
            careerId = career,
            courseId = await database.ScalarAsync<Guid>($"SELECT TOP 1 Id FROM courses WHERE CareerId = '{career}' ORDER BY Id"),
            teacherId = await database.ScalarAsync<Guid>("SELECT TOP 1 Id FROM employees WHERE IsTeacher = 1 ORDER BY Id"),
            facilityId = await database.ScalarAsync<Guid>("SELECT TOP 1 Id FROM SchoolFacility ORDER BY Id"),
            isTheory = true,
            capacity = 20,
            weekhours = 1.0,
            schedule = new[] { new { day = "Sábado", time = "20:00-21:00 hrs" } },
        };

        try
        {
            var responses = await Task.WhenAll(sessions.Select(s => s.PostAsync("data-capture/class-groups/create", request)));

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(11, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        }
        finally
        {
            await database.ExecuteAsync("DELETE FROM classGroups WHERE Schedule LIKE '%20:00-21:00 hrs%'");
        }
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
