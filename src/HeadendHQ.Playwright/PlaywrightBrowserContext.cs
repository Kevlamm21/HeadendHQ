using HeadendHQ.Core;
using Microsoft.Playwright;
using CoreBrowserContext = HeadendHQ.Core.IBrowserContext;
using PlaywrightContext = Microsoft.Playwright.IBrowserContext;

namespace HeadendHQ.Playwright;

internal sealed class PlaywrightBrowserContext(PlaywrightContext context) : CoreBrowserContext
{
    public async Task<IBrowserPage> NewPageAsync()
    {
        var page = await context.NewPageAsync();
        return new PlaywrightBrowserPage(page);
    }

    public async Task<string> ApiGetAsync(string url, IDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        var response = headers is not null
            ? await context.APIRequest.GetAsync(url, new() { Headers = headers })
            : await context.APIRequest.GetAsync(url);
        ct.ThrowIfCancellationRequested();
        if (!response.Ok)
            throw new HttpRequestException($"Browser API request to {url} failed with status {response.Status}.");
        return await response.TextAsync();
    }

    public async ValueTask DisposeAsync()
        => await context.DisposeAsync();
}
