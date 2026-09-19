using HeadendHQ.Core;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Transport;

internal sealed class HostLimiter(TransportProfile profile, ILogger logger) : IDisposable
{
    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(15);

    private readonly SemaphoreSlim _concurrency = new(Math.Max(1, profile.MaxConcurrency));
    private readonly Lock _pacing = new();
    private readonly Queue<DateTimeOffset> _thisMinute = new();
    private readonly Queue<DateTimeOffset> _thisHour = new();

    private DateTimeOffset _nextAllowed = DateTimeOffset.MinValue;
    private DateTimeOffset _cooldownUntil = DateTimeOffset.MinValue;

    public string Name => profile.Name;

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
    {
        ThrowIfCoolingDown();

        await _concurrency.WaitAsync(ct);
        try
        {
            await WaitForSlotAsync(ct);
            return await work(ct);
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private void ThrowIfCoolingDown()
    {
        DateTimeOffset until;
        lock (_pacing)
            until = _cooldownUntil;

        if (until > DateTimeOffset.UtcNow)
            throw new CatalogSourceThrottledException(
                $"{profile.Name} is cooling down after a rate limit until {until:HH:mm:ss}Z; skipping.");
    }

    private async Task WaitForSlotAsync(CancellationToken ct)
    {
        TimeSpan delay;

        lock (_pacing)
        {
            var now = DateTimeOffset.UtcNow;

            Drain(_thisMinute, now, TimeSpan.FromMinutes(1));
            Drain(_thisHour, now, TimeSpan.FromHours(1));

            if (profile.RequestsPerHour is { } budget && _thisHour.Count >= budget)
                throw new CatalogSourceThrottledException(
                    $"{profile.Name} has used its budget of {budget} request(s) in the last hour; stopping.");

            var earliest = _nextAllowed;

            if (_thisMinute.Count >= profile.RequestsPerMinute)
            {
                var bucketFreesAt = _thisMinute.Peek() + TimeSpan.FromMinutes(1);
                if (bucketFreesAt > earliest)
                    earliest = bucketFreesAt;
            }

            var sendAt = earliest > now ? earliest : now;
            var spacing = profile.MinDelayMs + Random.Shared.Next(0, Math.Max(1, profile.JitterMs));

            _nextAllowed = sendAt.AddMilliseconds(spacing);
            _thisMinute.Enqueue(sendAt);
            _thisHour.Enqueue(sendAt);
            delay = sendAt - now;
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
    }

    private static void Drain(Queue<DateTimeOffset> window, DateTimeOffset now, TimeSpan keep)
    {
        while (window.Count > 0 && now - window.Peek() > keep)
            window.Dequeue();
    }
    
    public void OnRateLimited(TimeSpan? retryAfter)
    {
        var cooldown = retryAfter is { } delta && delta > TimeSpan.Zero ? delta : DefaultCooldown;
        var until = DateTimeOffset.UtcNow + cooldown;

        lock (_pacing)
            _cooldownUntil = until;

        logger.LogWarning(
            "{Profile} returned 429; holding off until {Until:HH:mm:ss}Z ({Cooldown:0}s){Source}.",
            profile.Name, until, cooldown.TotalSeconds,
            retryAfter is null ? ", no Retry-After given" : " per Retry-After");

        throw new CatalogSourceThrottledException(
            $"{profile.Name} rate limited the client; backing off until {until:HH:mm:ss}Z.");
    }

    public void Dispose() => _concurrency.Dispose();
}
