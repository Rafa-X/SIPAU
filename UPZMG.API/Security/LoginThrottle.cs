using System.Collections.Concurrent;

namespace UPZMG.API.Security;

/// <summary>
/// Slows down repeated failed sign-ins for the same account.
///
/// The shared secret keeps strangers from calling web-login directly, but the public form on the
/// web is a legitimate caller, so without this a wrong password can be retried as fast as the
/// server can hash one — which is also the most expensive thing it does.
///
/// Counted per account rather than per address: the requests all arrive from the web server, so
/// its address says nothing. A successful sign-in clears the count.
/// </summary>
public sealed class LoginThrottle(ILogger<LoginThrottle> logger)
{
    private const int AllowedFailures = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockOut = TimeSpan.FromMinutes(5);

    // The university has tens of accounts. Anything near this many under way at once is not
    // people mistyping passwords, so the entries are swept and the flood is reported.
    private const int MaxTracked = 512;

    private readonly ConcurrentDictionary<string, Attempts> _attempts = new(StringComparer.OrdinalIgnoreCase);

    private sealed record Attempts(int Failures, DateTime FirstFailureUtc, DateTime? BlockedUntilUtc);

    private static bool Expired(Attempts state, DateTime now) =>
        (state.BlockedUntilUtc is null || state.BlockedUntilUtc <= now)
        && state.FirstFailureUtc + Window < now;

    /// <summary>How long the caller must wait, or null when it may try now.</summary>
    public TimeSpan? RetryAfter(string account)
    {
        if (string.IsNullOrWhiteSpace(account)) return null;
        if (!_attempts.TryGetValue(account, out var state)) return null;

        if (state.BlockedUntilUtc is { } until && until > DateTime.UtcNow)
            return until - DateTime.UtcNow;

        return null;
    }

    public void RegisterFailure(string account)
    {
        if (string.IsNullOrWhiteSpace(account)) return;

        var now = DateTime.UtcNow;

        // Nothing ever removed a failed account that simply gave up, so the table only grew.
        if (_attempts.Count >= MaxTracked) Sweep(now);

        var state = _attempts.AddOrUpdate(
            account,
            _ => new Attempts(1, now, null),
            (_, previous) => previous.FirstFailureUtc + Window < now
                ? new Attempts(1, now, null)               // the old streak expired
                : previous with { Failures = previous.Failures + 1 });

        if (state.Failures < AllowedFailures) return;

        _attempts[account] = state with { BlockedUntilUtc = now + LockOut };
        logger.LogWarning("Sign-in temporarily blocked for {Account} after {Failures} failed attempts.",
            account, state.Failures);
    }

    public void RegisterSuccess(string account)
    {
        if (!string.IsNullOrWhiteSpace(account)) _attempts.TryRemove(account, out _);
    }

    /// <summary>
    /// Drops the accounts whose streak has expired. When nearly nothing can be dropped, many
    /// different accounts are failing at once, which is what credential stuffing looks like
    /// from here; it is logged rather than turned into a lockout for everybody.
    /// </summary>
    private void Sweep(DateTime now)
    {
        var removed = 0;
        foreach (var (account, state) in _attempts)
        {
            if (Expired(state, now) && _attempts.TryRemove(account, out _)) removed++;
        }

        if (_attempts.Count >= MaxTracked)
        {
            logger.LogError(
                "Failed sign-ins are being tracked for {Accounts} different accounts "
                + "({Removed} expired entries released). This looks like an attack on the sign-in form.",
                _attempts.Count, removed);
        }
    }
}
