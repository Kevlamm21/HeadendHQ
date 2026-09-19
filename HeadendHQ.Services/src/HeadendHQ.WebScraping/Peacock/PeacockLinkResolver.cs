using HeadendHQ.Core;
using HeadendHQ.WebScraping.Transport;
using Microsoft.Playwright;

namespace HeadendHQ.WebScraping.Peacock;

internal sealed class PeacockLinkResolver(TransportRegistry transport) : ILinkResolver
{
    public Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawLink))
            return Task.FromResult<string?>(null);

        return transport.RunAsync(rawLink, async token =>
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(rawLink, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            token.ThrowIfCancellationRequested();

            var uri = new Uri(page.Url);
            return (string?)$"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
        }, ct);
    }
}
