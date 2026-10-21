using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UPZMG.IntegrationTests.Infrastructure;
using Xunit;

namespace UPZMG.IntegrationTests.Integration;

/// <summary>
/// The relations between students, careers, periods and registrations hold in two places: the
/// API refuses a record that points nowhere with a reason a person can read, and the database
/// refuses it even when the request does not come through the API at all.
/// </summary>
[Collection(SystemCollection.Name)]
[Trait("Category", "Integration")]
public sealed class IntegrityTests(TestDatabase database)
{
    private const int ForeignKeyViolation = 547;
    private const int DuplicateKey = 2627, DuplicateIndexRow = 2601;

    [SqlServerFact]
    public async Task AStudentOfACareerThatDoesNotExistIsRefused()
    {
        await using var api = new ApiHost(database);
        var session = await api.SignInAsync(Accounts.ControlEscolar, "User123!");
        var student = await Samples.StudentAsync(database, "IT-INT-0001");
        student.CareerId = Guid.NewGuid();

        var response = await session.PostAsync("data-capture/students/create", student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("carrera", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await Samples.StudentsWithAsync(database, "IT-INT-0001"));
    }

    [SqlServerFact]
    public async Task TheDatabaseItselfRefusesAStudentThatPointsNowhere()
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "UPDATE TOP (1) students SET CareerId = NEWID()"));
        Assert.Equal(ForeignKeyViolation, error.Number);

        error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "UPDATE TOP (1) students SET PeriodIdStart = NEWID()"));
        Assert.Equal(ForeignKeyViolation, error.Number);
    }

    [SqlServerFact]
    public async Task ACareerOrAPeriodInUseCannotBeDeleted()
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "DELETE FROM careers WHERE Id = (SELECT TOP 1 CareerId FROM students)"));
        Assert.Equal(ForeignKeyViolation, error.Number);

        error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "DELETE FROM periods WHERE Id = (SELECT TOP 1 PeriodId FROM kardex)"));
        Assert.Equal(ForeignKeyViolation, error.Number);
    }

    [SqlServerFact]
    public async Task AGradeOrARegistrationNeedsAnExistingStudent()
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "UPDATE TOP (1) kardex SET StudentId = NEWID()"));
        Assert.Equal(ForeignKeyViolation, error.Number);

        error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "UPDATE TOP (1) courseEnrollment SET StudentId = NEWID()"));
        Assert.Equal(ForeignKeyViolation, error.Number);
    }

    [SqlServerFact]
    public async Task TwoStudentsCannotShareAnEnrolmentNumberOrACurp()
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync("""
            UPDATE students SET EnrollmentNumber = (SELECT MIN(EnrollmentNumber) FROM students)
            WHERE EnrollmentNumber = (SELECT MAX(EnrollmentNumber) FROM students)
            """));
        Assert.Contains(error.Number, new[] { DuplicateKey, DuplicateIndexRow });

        error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync("""
            UPDATE students SET CURP = (SELECT MIN(CURP) FROM students WHERE CURP <> '')
            WHERE CURP = (SELECT MAX(CURP) FROM students WHERE CURP <> '')
            """));
        Assert.Contains(error.Number, new[] { DuplicateKey, DuplicateIndexRow });
    }

    [SqlServerFact]
    public async Task EveryStudentWithLanguagesOpensForCorrection()
    {
        // test-data.sql once stored a student's language as a bare id instead of the JSON list the
        // model reads, and opening any of those students for correction failed with a 500.
        Assert.Equal(0, await database.ScalarAsync<int>(
            "SELECT COUNT(*) FROM students WHERE ISJSON(IndigenousLanguages) = 0"));

        await using var api = new ApiHost(database);
        var session = await api.SignInAsync(Accounts.ControlEscolar, "User123!");
        await using var db = database.Context();
        var withLanguages = await db.Students
            .Where(s => s.IndigenousLanguages.Count > 0)
            .Select(s => s.Id)
            .ToListAsync();

        Assert.NotEmpty(withLanguages);
        foreach (var id in withLanguages)
        {
            var response = await session.Client.GetAsync($"data-capture/students/{id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
