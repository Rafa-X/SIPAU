using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Runs a read-then-write one at a time, across every process that shares the database.
///
/// Some rules have nothing a unique index can hold: "somebody can still administer", "this room
/// is free at this hour". Each request reads, decides and writes, so two at once both read the
/// state before the other's write and both go through. The lock belongs to the transaction and
/// ends with the commit, so the next request reads what this one wrote.
/// </summary>
public static class ExclusiveSection
{
    public const string Accounts = "sipau-accounts";
    public const string ClassGroups = "sipau-class-groups";

    private const int Busy = 51000;

    // Longer than any of these sections takes, shorter than the Web's limit on an API call.
    private const int WaitMilliseconds = 10_000;

    public static async Task<T> InExclusiveSectionAsync<T>(this DbContext db, string name, Func<Task<T>> work)
    {
        // The tests' in-memory provider has no transactions or locks, and no concurrent requests.
        if (!db.Database.IsRelational()) return await work();

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            // sp_getapplock reports a timeout as a negative result, not as an error.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource = {name}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {WaitMilliseconds};
                IF @result < 0 THROW 51000, 'Exclusive section busy.', 1;
                """);
        }
        catch (SqlException ex) when (ex.Number == Busy)
        {
            throw new InvalidOperationException("Hay otro cambio en curso. Vuelve a intentarlo en un momento.", ex);
        }

        var result = await work();
        await transaction.CommitAsync();
        return result;
    }

    public static Task InExclusiveSectionAsync(this DbContext db, string name, Func<Task> work) =>
        db.InExclusiveSectionAsync(name, async () =>
        {
            await work();
            return true;
        });
}
