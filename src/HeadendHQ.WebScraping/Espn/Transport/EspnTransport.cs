using System.Net;
using System.Net.Http.Headers;
using HeadendHQ.Core.Settings;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Transport;

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

            if (status == HttpStatusCode.Forbidden && EspnEndpoints.FallbackHost(target) is { } mirror)
            {
                logger.LogWarning("ESPN refused {Url}; retrying on the mirror host.", target);
                target = mirror;
                continue;
            }

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
        ApplyBrowserHeaders(request, SourceSettings.UserAgent);

        if (!string.IsNullOrEmpty(etag))
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag, isWeak: etag.StartsWith("W/", StringComparison.Ordinal)));

        if (lastModified is not null)
            request.Headers.IfModifiedSince = lastModified;

        return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

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
