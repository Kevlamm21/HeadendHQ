using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Espn.Transport;

/// <summary>
/// The single way anything in this assembly talks to ESPN.
/// <para>
/// The header set is load-bearing, not decoration. <c>site.api.espn.com</c> is fronted by Akamai bot
/// management which rejects a request that claims a browser User-Agent but arrives without the
/// headers a browser would send; supplying the full, self-consistent set is what turns a 403 into a
/// 200. That is also why every request — images included — goes through here rather than a bare
/// <see cref="HttpClient"/>.
/// </para>
/// </summary>
internal sealed class EspnTransport(HttpClient http, EspnRequestGate gate, ILogger<EspnTransport> logger)
{
    private const int MaxAttempts = 3;

    public async Task<string> GetStringAsync(string url, CancellationToken ct)
    {
        using var response = await SendAsync(url, etag: null, lastModified: null, ct);

        if (response is null)
            throw new InvalidOperationException($"ESPN unexpectedly reported no change for {url}.");

        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Issues a GET, retrying transient failures and falling back to the mirror host when the
    /// primary refuses us outright. Returns <c>null</c> only for a 304.
    /// </summary>
    public async Task<HttpResponseMessage?> SendAsync(
        string url, string? etag, DateTimeOffset? lastModified, CancellationToken ct)
    {
        var target = url;

        for (var attempt = 1; ; attempt++)
        {
            var response = await gate.RunAsync(token => SendOnceAsync(target, etag, lastModified, token), ct);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                response.Dispose();
                return null;
            }

            if (response.IsSuccessStatusCode)
                return response;

            var status = response.StatusCode;

            if (status == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta;
                response.Dispose();
                gate.OnRateLimited(retryAfter);
            }

            response.Dispose();

            // A 403 is Akamai, not a missing resource: the mirror host serves the same tree and is
            // worth one try before giving up on this URL.
            if (status == HttpStatusCode.Forbidden && EspnEndpoints.FallbackHost(target) is { } mirror)
            {
                logger.LogWarning("ESPN refused {Url}; retrying on the mirror host.", target);
                target = mirror;
                continue;
            }

            // Nothing about a 404 improves by asking again.
            if (status == HttpStatusCode.NotFound || attempt >= MaxAttempts)
                throw new HttpRequestException($"ESPN returned {(int)status} for {target}.", null, status);

            var backoff = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1) + Random.Shared.Next(0, 250));
            logger.LogWarning(
                "ESPN returned {Status} for {Url}; retry {Attempt}/{Max} in {Delay}ms.",
                (int)status, target, attempt, MaxAttempts, backoff.TotalMilliseconds);
            await Task.Delay(backoff, ct);
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        string url, string? etag, DateTimeOffset? lastModified, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyBrowserHeaders(request, (await gate.GetSettingsAsync(ct)).UserAgent);

        if (!string.IsNullOrEmpty(etag))
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag, isWeak: etag.StartsWith("W/", StringComparison.Ordinal)));

        if (lastModified is not null)
            request.Headers.IfModifiedSince = lastModified;

        return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>
    /// The header set proven to satisfy Akamai, applied to every single request so no call site can
    /// forget it — the previous implementation omitted these on image downloads, which is exactly the
    /// kind of inconsistency bot management looks for.
    /// </summary>
    private static void ApplyBrowserHeaders(HttpRequestMessage request, string userAgent)
    {
        var headers = request.Headers;
        headers.TryAddWithoutValidation("User-Agent", userAgent);
        headers.TryAddWithoutValidation("Accept", "*/*");
        headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        headers.TryAddWithoutValidation("DNT", "1");
        headers.TryAddWithoutValidation("Origin", "https://www.espn.com");
        headers.TryAddWithoutValidation("Referer", "https://www.espn.com/");
        headers.TryAddWithoutValidation("sec-ch-ua", "\"Chromium\";v=\"148\", \"Google Chrome\";v=\"148\", \"Not/A)Brand\";v=\"99\"");
        headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
        headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
        headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
        headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
        headers.TryAddWithoutValidation("sec-fetch-site", "same-site");
    }
}
