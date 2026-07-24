using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace HeadendHQ.Playwright;

public class BrowserSessionManager(IConfiguration configuration, ILogger<BrowserSessionManager> logger)
    : IBrowserSessionProvider, IHostedService, IAsyncDisposable
{
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly SemaphoreSlim _turnLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _sharedContext;
    private CancellationTokenSource? _idleCts;

    private TimeSpan IdleTimeout =>
        TimeSpan.FromSeconds(configuration.GetValue("Playwright:IdleTimeoutSeconds", 120));

    private int HumanDelayMinMs => configuration.GetValue("Playwright:HumanDelay:MinMs", 800);
    private int HumanDelayMaxMs => configuration.GetValue("Playwright:HumanDelay:MaxMs", 3500);

    public async Task<IBrowserSessionHandle> CreateSessionAsync(CancellationToken ct)
    {
        var context = await GetOrCreateSharedContextAsync(ct);

        await _turnLock.WaitAsync(ct);
        try
        {
            await DelayLikeAHumanAsync(ct);
        }
        catch
        {
            _turnLock.Release();
            throw;
        }

        return new BrowserSessionHandle(context, _turnLock);
    }

    private async Task DelayLikeAHumanAsync(CancellationToken ct)
    {
        var (min, max) = (HumanDelayMinMs, HumanDelayMaxMs);
        if (max <= min)
            return;

        var delay = TimeSpan.FromMilliseconds(Random.Shared.Next(min, max));
        await Task.Delay(delay, ct);
    }

    private async Task<IBrowserContext> GetOrCreateSharedContextAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_browser is null)
            {
                logger.LogInformation("Launching shared Chromium instance for browser automation.");

                _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    Args = ["--disable-blink-features=AutomationControlled"]
                });
            }

            _sharedContext ??= await _browser.NewContextAsync(new() { UserAgent = DefaultUserAgent });

            ResetIdleTimer();
            return _sharedContext;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void ResetIdleTimer()
    {
        _idleCts?.Cancel();
        _idleCts?.Dispose();

        var cts = new CancellationTokenSource();
        _idleCts = cts;
        var timeout = IdleTimeout;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(timeout, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await ShutdownBrowserAsync();
        });
    }

    private async Task ShutdownBrowserAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_browser is null)
                return;

            logger.LogInformation("Closing shared Chromium instance after {Timeout} of inactivity.", IdleTimeout);

            await _browser.CloseAsync();
            _browser = null;
            _sharedContext = null;

            _playwright?.Dispose();
            _playwright = null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _idleCts?.Cancel();
        await ShutdownBrowserAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _idleCts?.Cancel();
        await ShutdownBrowserAsync();
        _lock.Dispose();
        _turnLock.Dispose();
    }

    private sealed class BrowserSessionHandle(IBrowserContext context, SemaphoreSlim turnLock) : IBrowserSessionHandle
    {
        public IBrowserContext Context { get; } = context;

        public ValueTask DisposeAsync()
        {
            turnLock.Release();
            return ValueTask.CompletedTask;
        }
    }
}
