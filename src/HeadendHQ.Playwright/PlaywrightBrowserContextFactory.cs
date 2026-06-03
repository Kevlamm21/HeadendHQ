using HeadendHQ.Core.Browser;
using Microsoft.Playwright;

namespace HeadendHQ.Playwright;

public sealed class PlaywrightBrowserContextFactory : IBrowserContextFactory, IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task<Core.Browser.IBrowserContext> CreateAsync(BrowserContextOptions? options = null, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync(ct);
        var contextOptions = new BrowserNewContextOptions();
        if (options?.UserAgent is not null)
            contextOptions.UserAgent = options.UserAgent;
        var context = await browser.NewContextAsync(contextOptions);
        return new PlaywrightBrowserContext(context);
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser is not null) return _browser;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_browser is not null) return _browser;
            _playwright = await global::Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Args = ["--disable-blink-features=AutomationControlled"]
            });
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
