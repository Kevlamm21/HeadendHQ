using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Settings;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Transport;

public class EspnThrottledException(string message) : CatalogSourceThrottledException(message);

internal sealed class EspnRequestGate(ILogger<EspnRequestGate> logger) : IDisposable
{
    private readonly SemaphoreSlim _concurrency = new(Math.Max(1, SourceSettings.MaxConcurrency));
    private readonly Lock _pacing = new();
    private readonly Queue<DateTimeOffset> _recent = new();

    private DateTimeOffset _nextAllowed = DateTimeOffset.MinValue;
    private int _used;

    public int RequestsUsed => Volatile.Read(ref _used);

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
