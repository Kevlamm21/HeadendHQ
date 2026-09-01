using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Espn.Transport;

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
internal sealed class EspnRequestGate(IMediator mediator, ILogger<EspnRequestGate> logger) : IDisposable
{
    private readonly SemaphoreSlim _init = new(1, 1);
    private readonly Lock _pacing = new();
    private readonly Queue<DateTimeOffset> _recent = new();

    private SourceSettings? _settings;
    private SemaphoreSlim? _concurrency;
    private DateTimeOffset _nextAllowed = DateTimeOffset.MinValue;
    private int _used;

    public int RequestsUsed => Volatile.Read(ref _used);

    /// <summary>The pacing settings, loaded once per scope. Also carries the User-Agent to send.</summary>
    public Task<SourceSettings> GetSettingsAsync(CancellationToken ct) => EnsureSettingsAsync(ct);

    /// <summary>Waits until it is polite to send, then runs <paramref name="send"/>.</summary>
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> send, CancellationToken ct)
    {
        var settings = await EnsureSettingsAsync(ct);

        if (Interlocked.Increment(ref _used) > settings.PerRunRequestBudget)
            throw new EspnThrottledException(
                $"Request budget of {settings.PerRunRequestBudget} exhausted; stopping this run.");

        await _concurrency!.WaitAsync(ct);
        try
        {
            await WaitForSlotAsync(settings, ct);
            return await send(ct);
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private async Task<SourceSettings> EnsureSettingsAsync(CancellationToken ct)
    {
        if (_settings is not null)
            return _settings;

        await _init.WaitAsync(ct);
        try
        {
            if (_settings is null)
            {
                _settings = await mediator.Send(new GetSourceSettingsQuery(), ct);
                _concurrency = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrency));
            }
        }
        finally
        {
            _init.Release();
        }

        return _settings;
    }

    /// <summary>
    /// Folds the minimum spacing and the per-minute bucket into a single sleep, reserved under a lock
    /// so concurrent callers queue behind one another instead of all waking at the same instant.
    /// </summary>
    private async Task WaitForSlotAsync(SourceSettings settings, CancellationToken ct)
    {
        TimeSpan delay;

        lock (_pacing)
        {
            var now = DateTimeOffset.UtcNow;

            while (_recent.Count > 0 && now - _recent.Peek() > TimeSpan.FromMinutes(1))
                _recent.Dequeue();

            var earliest = _nextAllowed;

            if (_recent.Count >= settings.RequestsPerMinute)
            {
                var bucketFreesAt = _recent.Peek() + TimeSpan.FromMinutes(1);
                if (bucketFreesAt > earliest)
                    earliest = bucketFreesAt;
            }

            var sendAt = earliest > now ? earliest : now;
            var spacing = settings.MinDelayMs + Random.Shared.Next(0, Math.Max(1, settings.JitterMs));

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

    public void Dispose()
    {
        _concurrency?.Dispose();
        _init.Dispose();
    }
}
