using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace HeadendHQ.Nba;

public static class EspnLinkResolver
{
    private static readonly Regex GameIdPattern = new(@"[?&]gameId=(\d+)", RegexOptions.Compiled);
    private static readonly Regex StreamIdPattern = new(
        @"""strms"":\[(?:\{[^}]*\},)*?\{[^}]*""id"":""([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<string?> ResolveAsync(string smartLink, CancellationToken ct)
    {
        var gameIdMatch = GameIdPattern.Match(smartLink);
        if (!gameIdMatch.Success)
            return null;

        var gameId = gameIdMatch.Groups[1].Value;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        await using var context = await browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(
            $"https://www.espn.com/watch/player/_/eventCalendarId/{gameId}",
            new() { WaitUntil = WaitUntilState.Load });

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
