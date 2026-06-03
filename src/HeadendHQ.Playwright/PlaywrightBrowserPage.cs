using HeadendHQ.Core;
using Microsoft.Playwright;

namespace HeadendHQ.Playwright;

internal sealed class PlaywrightBrowserPage(IPage page) : IBrowserPage
{
    public string Url => page.Url;

    public async Task GotoAsync(string url, BrowserWaitUntil waitUntil = BrowserWaitUntil.Load)
        => await page.GotoAsync(url, new() { WaitUntil = ToPlaywrightState(waitUntil) });

    public async Task WaitForNetworkIdleAsync()
        => await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

    public async Task<string> GetContentAsync()
        => await page.ContentAsync();

    public async ValueTask DisposeAsync()
        => await page.CloseAsync();

    private static WaitUntilState ToPlaywrightState(BrowserWaitUntil waitUntil) => waitUntil switch
    {
        BrowserWaitUntil.DomContentLoaded => WaitUntilState.DOMContentLoaded,
        BrowserWaitUntil.NetworkIdle      => WaitUntilState.NetworkIdle,
        _                                 => WaitUntilState.Load
    };
}
