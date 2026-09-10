using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace UPZMG.Web.Services;

/// <summary>
/// Bridges the existing cookie authentication into Blazor's
/// <see cref="AuthenticationStateProvider"/>.
///
/// During the server prerender pass the authenticated principal is read from
/// <see cref="IHttpContextAccessor"/> and persisted with
/// <see cref="PersistentComponentState"/>; when the interactive circuit starts
/// (where the HttpContext is gone) the state is restored from that persisted
/// blob. This keeps <c>&lt;AuthorizeView&gt;</c> / <c>[Authorize]</c> working
/// across both render phases without introducing a second auth system.
///
/// A Blazor Server circuit is a single long-lived SignalR connection: once
/// the auth state is computed, nothing re-checks the cookie again unless a
/// new HTTP request happens (e.g. a full page reload). Without an explicit
/// timer, an open tab stays "logged in" forever, even past the cookie's
/// ExpireTimeSpan. A periodic timer re-checks the captured expiration and
/// forces sign-out once it elapses.
/// </summary>
public sealed class PersistentCookieAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private const string PersistenceKey = "sigie.auth.user";
    private static readonly TimeSpan RevalidationInterval = TimeSpan.FromMinutes(1);

    private readonly PersistentComponentState _state;
    private readonly IHttpContextAccessor _http;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly Timer? _revalidationTimer;
    private Task<AuthenticationState> _authState;
    private DateTimeOffset? _expiresUtc;

    public PersistentCookieAuthenticationStateProvider(
        PersistentComponentState state,
        IHttpContextAccessor http)
    {
        _state = state;
        _http = http;

        ClaimsPrincipal principal;

        if (_state.TryTakeFromJson<UserInfo>(PersistenceKey, out var restored) && restored is not null)
        {
            // Interactive circuit: rebuild the principal from persisted claims.
            principal = BuildPrincipal(restored);
            _expiresUtc = restored.ExpiresUtc;
        }
        else
        {
            // Prerender / SSR: read straight from the cookie-authenticated request.
            principal = _http.HttpContext?.User ?? Anonymous();
            _expiresUtc = GetCookieExpiration(_http.HttpContext);
        }

        _authState = Task.FromResult(new AuthenticationState(principal));
        _subscription = _state.RegisterOnPersisting(PersistAsync, RenderMode.InteractiveServer);

        if (principal.Identity?.IsAuthenticated == true && _expiresUtc.HasValue)
        {
            _revalidationTimer = new Timer(RevalidateExpiration, null, RevalidationInterval, RevalidationInterval);
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authState;

    /// Called by the client-side heartbeat once it has renewed the browser cookie
    /// via the keepalive endpoint, so this circuit's forced-logout clock matches it.
    public void ExtendSession(TimeSpan by) => _expiresUtc = DateTimeOffset.UtcNow + by;

    private void RevalidateExpiration(object? _)
    {
        if (_expiresUtc.HasValue && DateTimeOffset.UtcNow >= _expiresUtc.Value)
        {
            _authState = Task.FromResult(new AuthenticationState(Anonymous()));
            NotifyAuthenticationStateChanged(_authState);
            _revalidationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private Task PersistAsync()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            _state.PersistAsJson(PersistenceKey, new UserInfo(
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                GetCookieExpiration(_http.HttpContext)));
        }

        return Task.CompletedTask;
    }

    private static DateTimeOffset? GetCookieExpiration(HttpContext? context) =>
        context?.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Properties?.ExpiresUtc;

    private static ClaimsPrincipal BuildPrincipal(UserInfo info)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, info.UserId),
            new(ClaimTypes.Email, info.Email),
            new(ClaimTypes.Role, info.Role),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Cookies"));
    }

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public void Dispose()
    {
        _subscription.Dispose();
        _revalidationTimer?.Dispose();
    }

    private sealed record UserInfo(string UserId, string Email, string Role, DateTimeOffset? ExpiresUtc);
}
