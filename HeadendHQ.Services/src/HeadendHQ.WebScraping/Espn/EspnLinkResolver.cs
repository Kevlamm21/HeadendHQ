using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Settings;
using HeadendHQ.WebScraping.Transport;
using Microsoft.Playwright;

namespace HeadendHQ.WebScraping.Espn;

internal sealed class EspnLinkResolver(TransportRegistry transport) : ILinkResolver
{
    private static readonly Regex GameIdPattern = new(@"[?&]gameId=(\d+)", RegexOptions.Compiled);
    private static readonly Regex StreamIdPattern = new(
        @"""strms"":\[(?:\{[^}]*\},)*?\{[^}]*""id"":""([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawLink))
            return Task.FromResult<string?>(null);

        var gameIdMatch = GameIdPattern.Match(rawLink);
        if (!gameIdMatch.Success)
            return Task.FromResult<string?>(null);

        var gameId = gameIdMatch.Groups[1].Value;
        var watchUrl = $"https://www.espn.com/watch/player/_/eventCalendarId/{gameId}";

        return transport.RunAsync(watchUrl, token => ScrapeAsync(watchUrl, gameId, token), ct);
    }

    private static async Task<string?> ScrapeAsync(string watchUrl, string gameId, CancellationToken ct)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        await using var context = await browser.NewContextAsync(new()
        {
            UserAgent = SourceSettings.UserAgent
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(watchUrl, new() { WaitUntil = WaitUntilState.Load });

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        ct.ThrowIfCancellationRequested();

        var html = await page.ContentAsync();

        var evntIdIndex = html.IndexOf($"\"evntId\":{gameId}", StringComparison.Ordinal);
        if (evntIdIndex == -1)
            return null;

        var chunk = html.Substring(evntIdIndex, Math.Min(3000, html.Length - evntIdIndex));

        var match = StreamIdPattern.Match(chunk);
        return match.Success
            ? $"https://www.espn.com/watch/player/_/id/{match.Groups[1].Value}"
            : null;
    }
}
