using Microsoft.Playwright;

namespace HeadendHQ.Nba;

internal static class PeacockLinkResolver
{
    public static async Task<string> ResolveAsync(string shortLink, CancellationToken ct)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(shortLink, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        ct.ThrowIfCancellationRequested();

        var uri = new Uri(page.Url);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}
