namespace UPZMG.Web.Security;

// Single source of truth for the cookie idle-timeout policy, shared by
// Program.cs (cookie options) and the heartbeat/auth-state-provider pieces
// that keep the session alive while the user is active.
public static class SessionPolicy
{
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(15);
}
