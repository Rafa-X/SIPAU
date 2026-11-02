using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace UPZMG.Web.Services;

/// <summary>Whether the API stopped answering a moment ago, shared by every call of this Web.</summary>
public sealed class ApiLinkState
{
    private static readonly TimeSpan RestFor = TimeSpan.FromSeconds(10);
    private long _openUntilTicks;

    public bool IsOpen => DateTime.UtcNow.Ticks < Interlocked.Read(ref _openUntilTicks);

    public void Trip() => Interlocked.Exchange(ref _openUntilTicks, (DateTime.UtcNow + RestFor).Ticks);
}

/// <summary>
/// How long the Web waits for the API. .NET's default is 100 seconds per call, and one page makes
/// several, so an API that accepted connections and never answered left the person looking at a
/// blank page for minutes. A call that times out also makes the following ones fail at once for a
/// few seconds: the page learns the answer from the first one, not from each of them in turn.
/// </summary>
public sealed class ApiLinkHandler(ApiLinkState state, IConfiguration configuration, IMemoryCache cache) : DelegatingHandler
{
    // What legitimately takes long: a whole file stored in one transaction, and training on demand.
    private static readonly TimeSpan LongWork = TimeSpan.FromMinutes(3);

    private readonly TimeSpan _normal = TimeSpan.FromSeconds(configuration.GetValue("Api:TimeoutSeconds", 20));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (state.IsOpen)
            throw new HttpRequestException("The API timed out a moment ago.", null, HttpStatusCode.ServiceUnavailable);

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(LimitFor(request.RequestUri?.AbsolutePath ?? string.Empty));

        try
        {
            var response = await base.SendAsync(request, limit.Token);

            // A token kept for its whole lifetime is useless the moment the API changes its signing
            // key, and every call of that account would fail until it expired, signing in again
            // included. Forgetting it costs this one call; the next one asks for a new token.
            if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Authorization?.Parameter is { } token)
                ApiTokenService.Forget(cache, token);

            return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            state.Trip();
            throw new TaskCanceledException("The API did not answer in time.");
        }
    }

    private TimeSpan LimitFor(string path) =>
        path.EndsWith("/import", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/dropout/retrain", StringComparison.OrdinalIgnoreCase)
            ? LongWork
            : _normal;
}
