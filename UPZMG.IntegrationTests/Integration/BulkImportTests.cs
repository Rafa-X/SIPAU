using System.Net;
using UPZMG.Implementations.DataCapture;
using UPZMG.IntegrationTests.Infrastructure;
using Xunit;

namespace UPZMG.IntegrationTests.Integration;

/// <summary>
/// A file imported through the API: the check that writes nothing, the confirmation that stores
/// the accepted rows together, and a failure halfway through the write that must leave nothing.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Integration")]
public sealed class BulkImportTests(TestDatabase database) : IAsyncLifetime
{
    private ApiHost _api = null!;
    private ApiSession _session = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiHost(database);
        _session = await _api.SignInAsync(Accounts.ControlEscolar, "User123!");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private async Task<ImportResult> SendAsync(bool commit, params (int Line, CreateStudent Student)[] rows)
    {
        var response = await _session.PostAsync("data-capture/students/import", new ImportStudents
        {
            RequesterKey = _session.UserId,
            Commit = commit,
            Rows = [.. rows.Select(r => new ImportRow<CreateStudent> { Line = r.Line, Request = r.Student })],
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await _session.ReadAsync<ImportResult>(response);
    }

    private Task<int> StudentsAsync() => database.ScalarAsync<int>("SELECT COUNT(*) FROM students");

    [SqlServerFact]
    public async Task TheCheckWritesNothingAndGivesEveryRowItsReason()
    {
        var stored = await database.ScalarAsync<string>("SELECT TOP 1 EnrollmentNumber FROM students ORDER BY EnrollmentNumber");
        var valid = await Samples.StudentAsync(database, "IT-IMP-0002");
        var noName = await Samples.StudentAsync(database, "IT-IMP-0003");
        noName.Name = "";
        var repeated = await Samples.StudentAsync(database, "IT-IMP-0002");
        var alreadyStored = await Samples.StudentAsync(database, stored);
        var unknownLanguage = await Samples.StudentAsync(database, "IT-IMP-0006");
        unknownLanguage.IndigenousLanguages = ["Klingon"];
        var shortCurp = await Samples.StudentAsync(database, "IT-IMP-0007", "ABC");
        var before = await StudentsAsync();

        var result = await SendAsync(commit: false,
            (2, valid), (3, noName), (4, repeated), (5, alreadyStored), (6, unknownLanguage), (7, shortCurp));

        Assert.False(result.Committed);
        Assert.Equal(before, await StudentsAsync());

        Assert.True(Row(result, 2).Accepted);
        Assert.Contains("Falta el nombre.", Row(result, 3).Problems);
        Assert.Contains(Row(result, 4).Problems, p => p.Contains("ya aparece en la línea 2"));
        Assert.Contains(Row(result, 5).Problems, p => p.Contains("Ya existe un alumno con la matrícula"));
        Assert.Contains(Row(result, 6).Problems, p => p.Contains("Klingon"));
        Assert.Contains("La CURP debe tener 18 caracteres.", Row(result, 7).Problems);
        Assert.Equal(1, result.Accepted);
        Assert.Equal(5, result.Refused);
    }

    [SqlServerFact]
    public async Task ConfirmedRowsAreStoredTogetherAndListedBack()
    {
        var first = await Samples.StudentAsync(database, "IT-IMP-0101");
        var second = await Samples.StudentAsync(database, "IT-IMP-0102", "IMPR050520MJCRRS02");
        var before = await StudentsAsync();

        var result = await SendAsync(commit: true, (2, first), (3, second));

        Assert.True(result.Committed, result.Message);
        Assert.Equal(["IT-IMP-0101", "IT-IMP-0102"], result.Rows.Select(r => r.Key));
        Assert.Equal(before + 2, await StudentsAsync());

        // Importing the same file again is refused whole: nothing is duplicated.
        var again = await SendAsync(commit: true, (2, first), (3, second));
        Assert.False(again.Committed);
        Assert.All(again.Rows, r => Assert.Contains(r.Problems, p => p.Contains("Ya existe un alumno")));
        Assert.Equal(before + 2, await StudentsAsync());
    }

    [SqlServerFact]
    public async Task AFailureHalfwayThroughTheWriteLeavesNothingBehind()
    {
        // The database itself refuses one row while the batch is being written, after every
        // check passed: the rows written before it must be rolled back with it.
        await database.ExecuteAsync("""
            CREATE OR ALTER TRIGGER it_refuse_one_student ON students AFTER INSERT AS
            IF EXISTS (SELECT 1 FROM inserted WHERE EnrollmentNumber = 'IT-IMP-FALLA')
                THROW 50001, 'Rechazo de prueba', 1;
            """);

        try
        {
            var rows = new[]
            {
                (2, await Samples.StudentAsync(database, "IT-IMP-0201")),
                (3, await Samples.StudentAsync(database, "IT-IMP-0202")),
                (4, await Samples.StudentAsync(database, "IT-IMP-FALLA")),
                (5, await Samples.StudentAsync(database, "IT-IMP-0204")),
            };
            var before = await StudentsAsync();

            var result = await SendAsync(commit: true, rows);

            Assert.False(result.Committed);
            Assert.Contains("línea 4", result.Message);
            Assert.False(Row(result, 4).Accepted);
            Assert.Equal(before, await StudentsAsync());
            Assert.Equal(0, await Samples.StudentsWithAsync(database, "IT-IMP-0201"));
            Assert.Equal(0, await Samples.StudentsWithAsync(database, "IT-IMP-0202"));
        }
        finally
        {
            await database.ExecuteAsync("DROP TRIGGER IF EXISTS it_refuse_one_student;");
        }
    }

    private static ImportRowResult Row(ImportResult result, int line) => Assert.Single(result.Rows, r => r.Line == line);
}
