using System.Security.Claims;
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
/// </summary>
public sealed class PersistentCookieAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private const string PersistenceKey = "sigie.auth.user";

    private readonly PersistentComponentState _state;
    private readonly IHttpContextAccessor _http;
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly Task<AuthenticationState> _authState;

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
        }
        else
        {
            // Prerender / SSR: read straight from the cookie-authenticated request.
            principal = _http.HttpContext?.User ?? Anonymous();
        }

        _authState = Task.FromResult(new AuthenticationState(principal));
        _subscription = _state.RegisterOnPersisting(PersistAsync, RenderMode.InteractiveServer);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authState;

    private Task PersistAsync()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            _state.PersistAsJson(PersistenceKey, new UserInfo(
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty));
        }

        return Task.CompletedTask;
    }

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

    public void Dispose() => _subscription.Dispose();

    private sealed record UserInfo(string UserId, string Email, string Role);
}
