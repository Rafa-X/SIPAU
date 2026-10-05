using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// Turns a unique-index violation into the same refusal the repository would have produced had
/// it seen the duplicate first.
///
/// The checks before inserting catch the ordinary case and explain it well; the index catches the
/// case two requests arrive at once. Without this, that second one reaches the area as a server
/// error, which reads like a broken system rather than "ese registro ya existe".
/// </summary>
public static class UniqueViolation
{
    private const int DuplicateKey = 2627;      // violation of a unique constraint
    private const int DuplicateIndexRow = 2601; // duplicate row in a unique index

    /// <summary>Saves, translating a duplicate into <paramref name="message"/>.</summary>
    public static async Task SaveGuardingUniquenessAsync(this DbContext db, string message)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            throw new InvalidOperationException(message, ex);
        }
    }

    public static bool IsDuplicate(DbUpdateException exception) =>
        exception.InnerException is SqlException sql &&
        sql.Number is DuplicateKey or DuplicateIndexRow;
}
