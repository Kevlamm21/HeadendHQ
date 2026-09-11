using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Settings;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Transport;

public class EspnThrottledException(string message) : CatalogSourceThrottledException(message);

internal sealed record GateLimits(
    string Name, int RequestsPerMinute, int MinDelayMs, int JitterMs, int MaxConcurrency, int? Budget);

internal abstract class EspnRequestGate(GateLimits limits, ILogger logger) : IDisposable
{
    private readonly SemaphoreSlim _concurrency = new(Math.Max(1, limits.MaxConcurrency));
    private readonly Lock _pacing = new();
    private readonly Queue<DateTimeOffset> _recent = new();

    private DateTimeOffset _nextAllowed = DateTimeOffset.MinValue;
    private int _used;
    private volatile bool _tripped;

    public int RequestsUsed => Volatile.Read(ref _used);

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> send, CancellationToken ct)
    {
        if (_tripped)
            throw new EspnThrottledException($"ESPN {limits.Name} rate limited this run earlier; skipping.");

        if (Interlocked.Increment(ref _used) > limits.Budget)
            throw new EspnThrottledException(
                $"ESPN {limits.Name} request budget of {limits.Budget} exhausted; stopping this run.");

        await _concurrency.WaitAsync(ct);
        try
        {
            await WaitForSlotAsync(ct);
            return await send(ct);
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private async Task WaitForSlotAsync(CancellationToken ct)
    {
        TimeSpan delay;

        lock (_pacing)
        {
            var now = DateTimeOffset.UtcNow;

            while (_recent.Count > 0 && now - _recent.Peek() > TimeSpan.FromMinutes(1))
                _recent.Dequeue();

            var earliest = _nextAllowed;

            if (_recent.Count >= limits.RequestsPerMinute)
            {
                var bucketFreesAt = _recent.Peek() + TimeSpan.FromMinutes(1);
                if (bucketFreesAt > earliest)
                    earliest = bucketFreesAt;
            }

            var sendAt = earliest > now ? earliest : now;
            var spacing = limits.MinDelayMs + Random.Shared.Next(0, Math.Max(1, limits.JitterMs));

            _nextAllowed = sendAt.AddMilliseconds(spacing);
            _recent.Enqueue(sendAt);
            delay = sendAt - now;
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
    }

    public void OnRateLimited(TimeSpan? retryAfter)
    {
        _tripped = true;

        logger.LogWarning(
            "ESPN {Gate} returned 429 after {Used} request(s){RetryAfter}. Ending this run.",
            limits.Name,
            RequestsUsed,
            retryAfter is null ? "" : $"; Retry-After {retryAfter.Value.TotalSeconds:0}s");

        throw new EspnThrottledException($"ESPN {limits.Name} rate limited the client; run stopped.");
    }

    public void Dispose() => _concurrency.Dispose();
}

internal sealed class EspnApiGate(ILogger<EspnApiGate> logger) : EspnRequestGate(
    new GateLimits(
        "API", SourceSettings.RequestsPerMinute, SourceSettings.MinDelayMs, SourceSettings.JitterMs,
        SourceSettings.MaxConcurrency, SourceSettings.PerRunRequestBudget),
    logger);

internal sealed class EspnCdnGate(ILogger<EspnCdnGate> logger) : EspnRequestGate(
    new GateLimits(
        "CDN", SourceSettings.CdnRequestsPerMinute, SourceSettings.CdnMinDelayMs, SourceSettings.CdnJitterMs,
        SourceSettings.CdnMaxConcurrency, Budget: null),
    logger);
