using HeadendHQ.Core;
using HeadendHQ.Core.Titles;
using Microsoft.Playwright;

namespace HeadendHQ.Peacock;

public class PeacockLinkResolver : ILinkResolver
{
    public StreamingService Service => StreamingService.Peacock;

    public async Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawLink))
            return null;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(rawLink, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        ct.ThrowIfCancellationRequested();

        var uri = new Uri(page.Url);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}
