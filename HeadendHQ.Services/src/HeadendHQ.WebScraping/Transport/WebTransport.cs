using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Transport;

internal sealed class WebTransport(
    HttpClient http, TransportRegistry registry, ILogger<WebTransport> logger)
{
    public async Task<string> GetStringAsync(string url, CancellationToken ct)
    {
        using var response = await SendAsync(url, etag: null, lastModified: null, ct)
            ?? throw new InvalidOperationException($"Unexpected 304 for {url}; no cache validators were sent.");

        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<HttpResponseMessage?> SendAsync(
        string url, string? etag, DateTimeOffset? lastModified, CancellationToken ct)
    {
        var profile = registry.For(url);
        var limiter = registry.LimiterFor(url);
        var target = url;

        for (var attempt = 1; ; attempt++)
        {
            var response = await limiter.RunAsync(
                token => SendOnceAsync(target, profile, etag, lastModified, token), ct);

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
                limiter.OnRateLimited(retryAfter);
            }

            response.Dispose();

            if (status == HttpStatusCode.Forbidden && profile.FallbackHost?.Invoke(target) is { } mirror)
            {
                logger.LogWarning("{Profile} refused {Url}; retrying on the mirror host.", profile.Name, target);
                target = mirror;
                continue;
            }

            if (status == HttpStatusCode.NotFound || attempt >= profile.MaxAttempts)
                throw new HttpRequestException($"{profile.Name} returned {(int)status} for {target}.", null, status);

            var backoff = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1) + Random.Shared.Next(0, 250));
            logger.LogWarning(
                "{Profile} returned {Status} for {Url}; retry {Attempt}/{Max} in {Delay}ms.",
                profile.Name, (int)status, target, attempt, profile.MaxAttempts, backoff.TotalMilliseconds);
            await Task.Delay(backoff, ct);
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        string url, TransportProfile profile, string? etag, DateTimeOffset? lastModified, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        foreach (var (name, value) in profile.Headers ?? TransportProfile.Default.Headers!)
            request.Headers.TryAddWithoutValidation(name, value);

        if (!string.IsNullOrEmpty(etag))
            request.Headers.IfNoneMatch.Add(
                new EntityTagHeaderValue(etag, isWeak: etag.StartsWith("W/", StringComparison.Ordinal)));

        if (lastModified is not null)
            request.Headers.IfModifiedSince = lastModified;

        return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}
