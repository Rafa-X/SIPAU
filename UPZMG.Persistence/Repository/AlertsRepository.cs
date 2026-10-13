using Microsoft.EntityFrameworkCore;
using UPZMG.Implementations.Intelligence;
using UPZMG.Persistence.Models;

namespace UPZMG.Persistence.Repository;

/// <summary>
/// The alerts of the intelligent layer as each user left them.
///
/// The deviations are detected from the series and are the same for everybody. What is stored
/// here is only the personal part: which ones a user has seen and which ones they dismissed, so
/// the bell can count what is new for that person instead of everything detected.
/// </summary>
public class AlertsRepository(AppDBContext db, IntelligenceRepository intelligenceRepository)
{
    // Current alerts with the user's marks applied
    public async Task<AlertInbox> GetInboxAsync(Guid userId)
    {
        var alerts = await intelligenceRepository.GetAnomaliesAsync();

        var receipts = await db.AlertReceipts
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new AlertReceiptState(r.AlertKey, r.SeenAtUtc != null, r.DismissedAtUtc != null))
            .ToListAsync();

        return AlertInboxBuilder.Build(alerts, receipts);
    }

    // Marks the given alerts as seen
    public Task<AlertInbox> MarkSeenAsync(Guid userId, IReadOnlyCollection<string> keys) =>
        StampAsync(userId, keys, dismiss: false);

    // Takes the given alerts out of the user's list; a dismissed alert counts as seen
    public Task<AlertInbox> DismissAsync(Guid userId, IReadOnlyCollection<string> keys) =>
        StampAsync(userId, keys, dismiss: true);

    // Brings back everything the user dismissed. They return as seen: it is not news.
    public async Task<AlertInbox> RestoreAsync(Guid userId)
    {
        await db.AlertReceipts
            .Where(r => r.UserId == userId && r.DismissedAtUtc != null)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.DismissedAtUtc, (DateTime?)null));

        return await GetInboxAsync(userId);
    }

    private async Task<AlertInbox> StampAsync(Guid userId, IReadOnlyCollection<string> keys, bool dismiss)
    {
        // Only alerts that exist right now can be marked, so the table holds nothing a caller made up.
        var current = (await intelligenceRepository.GetAnomaliesAsync()).Select(a => a.Key).ToHashSet();
        var wanted = keys.Where(current.Contains).Distinct().ToList();

        if (wanted.Count > 0)
        {
            var now = DateTime.UtcNow;
            var stored = await db.AlertReceipts
                .Where(r => r.UserId == userId && wanted.Contains(r.AlertKey))
                .ToListAsync();

            foreach (var key in wanted)
            {
                var receipt = stored.FirstOrDefault(r => r.AlertKey == key);
                if (receipt is null)
                {
                    receipt = new AlertReceipt { Id = Guid.NewGuid(), UserId = userId, AlertKey = key };
                    db.AlertReceipts.Add(receipt);
                }

                receipt.SeenAtUtc ??= now;
                if (dismiss) receipt.DismissedAtUtc ??= now;
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The same person marking the same alert from two tabs at once: the unique index
                // refused the second row, and the first one already says what this call wanted.
                db.ChangeTracker.Clear();
            }
        }

        return await GetInboxAsync(userId);
    }
}
