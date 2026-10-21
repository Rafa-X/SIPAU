using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UPZMG.Persistence;
using Xunit;

namespace UPZMG.IntegrationTests.Infrastructure;

/// <summary>
/// A database of its own for one test run, on a real SQL Server: created from the migrations,
/// loaded with the same seeds as the development database, and dropped at the end. Nothing here
/// touches the database the team works with.
///
/// The server comes from <see cref="ServerVariable"/>, a connection string for an account that
/// may create databases (the container's sa). Without it the tests that need it are skipped.
/// </summary>
public sealed class TestDatabase : IAsyncLifetime
{
    public const string ServerVariable = "UPZMG_IT_SQL";

    // Set to 1 to keep the database after the run and look at what the tests left.
    public const string KeepVariable = "UPZMG_IT_KEEP_DB";

    private static readonly string[] Seeds =
    [
        "test-data.sql",
        "seed-official-catalogs.sql",
        "seed-permissions.sql",
        "seed-academic-history.sql",
        "seed-report-captures.sql",
    ];

    public static string? Server => Environment.GetEnvironmentVariable(ServerVariable);

    public string Name { get; } = $"upzmg_it_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(1000, 9999)}";

    public string ConnectionString { get; private set; } = string.Empty;

    // Fresh secrets for every run: the API and the Web of a run trust each other and nothing else.
    public string JwtKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public string SharedSecret { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public async Task InitializeAsync()
    {
        if (Server is null) return;

        ConnectionString = new SqlConnectionStringBuilder(Server) { InitialCatalog = Name }.ConnectionString;

        // The migrations create the database and its schema exactly as the team's.
        await using (var db = Context()) await db.Database.MigrateAsync();

        foreach (var seed in Seeds)
        {
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Seeds", seed), Encoding.UTF8);
            await ExecuteAsync(script, timeoutSeconds: 600);
        }
    }

    public async Task DisposeAsync()
    {
        if (Server is null || ConnectionString.Length == 0) return;
        if (Environment.GetEnvironmentVariable(KeepVariable) == "1") return;

        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(new SqlConnectionStringBuilder(Server) { InitialCatalog = "master" }.ConnectionString);
        await master.OpenAsync();
        await using var drop = new SqlCommand(
            $"IF DB_ID(N'{Name}') IS NOT NULL BEGIN ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Name}]; END",
            master);
        await drop.ExecuteNonQueryAsync();
    }

    public AppDBContext Context() =>
        new(new DbContextOptionsBuilder<AppDBContext>().UseSqlServer(ConnectionString).Options);

    public async Task ExecuteAsync(string sql, int timeoutSeconds = 60, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = timeoutSeconds };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (T)Convert.ChangeType((await command.ExecuteScalarAsync())!, typeof(T));
    }
}

/// <summary>Every test class that needs the database shares one, created once per run.</summary>
[CollectionDefinition(Name)]
public sealed class SystemCollection : ICollectionFixture<TestDatabase>
{
    public const string Name = "Sistema completo";
}

/// <summary>A test that needs a SQL Server; skipped, saying why, when none was given.</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (TestDatabase.Server is null)
            Skip = $"Define {TestDatabase.ServerVariable} con la cadena de conexión de un SQL Server donde se puedan crear bases.";
    }
}

/// <summary>A theory that needs a SQL Server; skipped like <see cref="SqlServerFactAttribute"/>.</summary>
public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (TestDatabase.Server is null)
            Skip = $"Define {TestDatabase.ServerVariable} con la cadena de conexión de un SQL Server donde se puedan crear bases.";
    }
}
