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

    private readonly ConcurrentDictionary<string, Attempts> _attempts = new(StringComparer.OrdinalIgnoreCase);

    private sealed record Attempts(int Failures, DateTime FirstFailureUtc, DateTime? BlockedUntilUtc);

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
}
