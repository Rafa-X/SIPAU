using System.Net;
using Microsoft.Data.SqlClient;
using UPZMG.IntegrationTests.Infrastructure;
using Xunit;

namespace UPZMG.IntegrationTests.Integration;

/// <summary>
/// What each area may read and write, asked of the running API with each area's own account.
/// The expected matrix is written out here on purpose, apart from the code that enforces it:
/// it is the access policy the university signed off, and a change to it should fail a test.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AccessControlTests(TestDatabase database) : IAsyncLifetime
{
    private ApiHost _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiHost(database);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private const string A = Accounts.Admin, CE = Accounts.ControlEscolar, SE = Accounts.ServiciosEstudiantiles,
        RH = Accounts.RecursosHumanos, DA = Accounts.DireccionAcademica, RM = Accounts.RecursosMateriales;

    // Endpoint → the accounts that may use it. Everybody else must get 403.
    private static readonly (string Method, string Path, string[] Allowed)[] Matrix =
    [
        ("GET", $"data-capture/students/list?RequesterKey={Guid.Empty}&Page=1&PageSize=5", [A, CE, SE]),
        ("GET", "data-capture/employees/teachers", [A, RH]),
        ("GET", "data-capture/class-groups/list", [A, DA]),
        ("GET", "data-capture/facilities/list", [A, DA, RM]),
        ("GET", "intelligence/dropout", [A, CE, SE, DA]),
        ("GET", "intelligence/forecast", [A, CE, SE, RH, DA, RM]),
        ("GET", "reports/templates", [A, CE, SE, RH, DA, RM]),
        ("GET", "boardreport/cycles", [A, CE, SE, RH, DA, RM]),
        ("POST", "users/get-all", [A]),
        ("GET", "periods/list", [A]),
    ];

    public static TheoryData<string> Areas => [A, CE, SE, RH, DA, RM];

    [SqlServerTheory]
    [MemberData(nameof(Areas))]
    public async Task EachAreaReachesWhatItsRoleGrantsAndNothingElse(string account)
    {
        var session = await _api.SignInAsync(account, Accounts.PasswordOf(account));
        var wrong = new List<string>();

        foreach (var (method, path, allowed) in Matrix)
        {
            var response = method == "GET"
                ? await session.Client.GetAsync(path)
                : await session.PostAsync(path, new { requesterKey = session.UserId });

            var expected = allowed.Contains(account) ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
            if (response.StatusCode != expected)
                wrong.Add($"{method} {path}: se esperaba {(int)expected} y respondió {(int)response.StatusCode}");
        }

        Assert.True(wrong.Count == 0, $"{account}:\n" + string.Join("\n", wrong));
    }

    [SqlServerFact]
    public async Task WithoutATokenNothingIsServed()
    {
        var anonymous = _api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("intelligence/forecast")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("data-capture/employees/teachers")).StatusCode);
    }

    [SqlServerFact]
    public async Task AnAreaCannotWriteIntoAnotherAreasRecords()
    {
        // Recursos Humanos registering a student, Control Escolar registering an employee.
        var rh = await _api.SignInAsync(RH, Accounts.PasswordOf(RH));
        var student = await Samples.StudentAsync(database, "IT-CRUCE-01");
        Assert.Equal(HttpStatusCode.Forbidden, (await rh.PostAsync("data-capture/students/create", student)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rh.PostAsync("data-capture/students/import",
            new { requesterKey = rh.UserId, commit = true, rows = new[] { new { line = 2, request = student } } })).StatusCode);
        Assert.Equal(0, await Samples.StudentsWithAsync(database, "IT-CRUCE-01"));

        var ce = await _api.SignInAsync(CE, Accounts.PasswordOf(CE));
        var employee = Samples.Employee(990101, "cruce");
        Assert.Equal(HttpStatusCode.Forbidden, (await ce.PostAsync("data-capture/employees/create", employee)).StatusCode);
        Assert.Equal(0, await database.ScalarAsync<int>("SELECT COUNT(*) FROM employees WHERE EmployeeNumber = 990101"));
    }

    [SqlServerFact]
    public async Task AWithdrawnPermissionAppliesOnTheVeryNextRequest()
    {
        var admin = await _api.SignInAsync(A, Accounts.PasswordOf(A));
        var ce = await _api.SignInAsync(CE, Accounts.PasswordOf(CE));
        Assert.Equal(HttpStatusCode.OK, (await ce.Client.GetAsync("data-capture/students/list?RequesterKey=" + Guid.Empty)).StatusCode);

        var role = await database.ScalarAsync<Guid>("SELECT Id FROM roles WHERE Name = N'Control Escolar'");
        var granted = await PermissionsOfAsync(role);
        var studentsPermission = await database.ScalarAsync<Guid>("SELECT Id FROM permissions WHERE Name = N'alumnos.capturar'");

        try
        {
            var withdraw = await admin.PostAsync("roles/set-permissions",
                new { requesterKey = admin.UserId, roleId = role, permissionIds = granted.Where(p => p != studentsPermission).ToList() });
            Assert.Equal(HttpStatusCode.OK, withdraw.StatusCode);

            // Same token, issued before the change: the API asks the database on every request.
            Assert.Equal(HttpStatusCode.Forbidden,
                (await ce.Client.GetAsync("data-capture/students/list?RequesterKey=" + Guid.Empty)).StatusCode);
        }
        finally
        {
            await admin.PostAsync("roles/set-permissions", new { requesterKey = admin.UserId, roleId = role, permissionIds = granted });
        }

        Assert.Equal(HttpStatusCode.OK, (await ce.Client.GetAsync("data-capture/students/list?RequesterKey=" + Guid.Empty)).StatusCode);
    }

    [SqlServerFact]
    public async Task ASuspendedAccountLosesAccessAndCannotGetANewToken()
    {
        var admin = await _api.SignInAsync(A, Accounts.PasswordOf(A));
        var rm = await _api.SignInAsync(RM, Accounts.PasswordOf(RM));
        Assert.Equal(HttpStatusCode.OK, (await rm.Client.GetAsync("data-capture/facilities/list")).StatusCode);

        try
        {
            var suspend = await admin.PostAsync("users/set-active", new { requesterKey = admin.UserId, userId = rm.UserId, isActive = false });
            Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);

            Assert.Equal(HttpStatusCode.Forbidden, (await rm.Client.GetAsync("data-capture/facilities/list")).StatusCode);

            // The token exchange the Web performs on its behalf is refused as well.
            var exchange = await _api.CreateClient().PostAsync("auth/token", JsonBody(new { userId = rm.UserId, sharedSecret = database.SharedSecret }));
            Assert.Equal(HttpStatusCode.Forbidden, exchange.StatusCode);

            // And the Web learns it from the permissions call too, with the token it still holds.
            Assert.Equal(HttpStatusCode.Forbidden, (await rm.Client.GetAsync("auth/permissions")).StatusCode);
        }
        finally
        {
            await admin.PostAsync("users/set-active", new { requesterKey = admin.UserId, userId = rm.UserId, isActive = true });
        }
    }

    [SqlServerFact]
    public async Task OnlyAProcessThatKnowsTheSharedSecretCanSignSomebodyIn()
    {
        var client = _api.CreateClient();
        var forged = await client.PostAsync("auth/web-login",
            JsonBody(new { email = A, password = Accounts.PasswordOf(A), sharedSecret = "un-secreto-que-no-es" }));

        // 403, not the 401 of a wrong password: the Web must not blame the person's password for
        // its own configuration.
        Assert.Equal(HttpStatusCode.Forbidden, forged.StatusCode);
    }

    private async Task<List<Guid>> PermissionsOfAsync(Guid role)
    {
        var ids = new List<Guid>();
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT PermissionId FROM rolePermissions WHERE RoleId = @r", connection);
        command.Parameters.AddWithValue("@r", role);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) ids.Add(reader.GetGuid(0));
        return ids;
    }

    private static StringContent JsonBody(object body) =>
        new(System.Text.Json.JsonSerializer.Serialize(body, ApiHost.Json), System.Text.Encoding.UTF8, "application/json");
}
