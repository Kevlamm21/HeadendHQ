using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Settings;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Transport;

/// <summary>
/// Raised when a run exhausts its request budget or ESPN asks us to back off. Both are terminal:
/// the respectful reading of a 429 is to stop for tonight, not to try harder.
/// </summary>
public class EspnThrottledException(string message) : CatalogSourceThrottledException(message);

/// <summary>
/// Paces every outbound ESPN request so a scrape reads like a person browsing rather than a crawler:
/// at most a couple in flight, never closer together than a few hundred milliseconds, jittered so the
/// cadence is not perfectly regular, capped per minute, and capped in total.
/// <para>
/// Registered scoped, so the request budget covers one unit of work — a nightly scrape, a discovery
/// run, one API call — and a runaway loop is bounded rather than unbounded.
/// </para>
/// </summary>
internal sealed class EspnRequestGate(ILogger<EspnRequestGate> logger) : IDisposable
{
    private readonly SemaphoreSlim _concurrency = new(Math.Max(1, SourceSettings.MaxConcurrency));
    private readonly Lock _pacing = new();
    private readonly Queue<DateTimeOffset> _recent = new();

    private DateTimeOffset _nextAllowed = DateTimeOffset.MinValue;
    private int _used;

    public int RequestsUsed => Volatile.Read(ref _used);

    /// <summary>Waits until it is polite to send, then runs <paramref name="send"/>.</summary>
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> send, CancellationToken ct)
    {
        if (Interlocked.Increment(ref _used) > SourceSettings.PerRunRequestBudget)
            throw new EspnThrottledException(
                $"Request budget of {SourceSettings.PerRunRequestBudget} exhausted; stopping this run.");

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

    /// <summary>
    /// Folds the minimum spacing and the per-minute bucket into a single sleep, reserved under a lock
    /// so concurrent callers queue behind one another instead of all waking at the same instant.
    /// </summary>
    private async Task WaitForSlotAsync(CancellationToken ct)
    {
        TimeSpan delay;

        lock (_pacing)
        {
            var now = DateTimeOffset.UtcNow;

            while (_recent.Count > 0 && now - _recent.Peek() > TimeSpan.FromMinutes(1))
                _recent.Dequeue();

            var earliest = _nextAllowed;

            if (_recent.Count >= SourceSettings.RequestsPerMinute)
            {
                var bucketFreesAt = _recent.Peek() + TimeSpan.FromMinutes(1);
                if (bucketFreesAt > earliest)
                    earliest = bucketFreesAt;
            }

            var sendAt = earliest > now ? earliest : now;
            var spacing = SourceSettings.MinDelayMs
                + Random.Shared.Next(0, Math.Max(1, SourceSettings.JitterMs));

            _nextAllowed = sendAt.AddMilliseconds(spacing);
            _recent.Enqueue(sendAt);
            delay = sendAt - now;
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
    }

    public void OnRateLimited(TimeSpan? retryAfter)
    {
        logger.LogWarning(
            "ESPN returned 429 after {Used} request(s){RetryAfter}. Ending this run.",
            RequestsUsed,
            retryAfter is null ? "" : $"; Retry-After {retryAfter.Value.TotalSeconds:0}s");

        throw new EspnThrottledException("ESPN rate limited the client; run stopped.");
    }

    public void Dispose() => _concurrency.Dispose();
}
