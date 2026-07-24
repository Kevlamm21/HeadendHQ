using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;
using HeadendHQ.Playwright;

namespace HeadendHQ.Espn;

public class EspnLinkResolver(IBrowserSessionProvider sessionProvider) : ILinkResolver
{
    private static readonly Regex GameIdPattern = new(@"[?&]gameId=(\d+)", RegexOptions.Compiled);
    private static readonly Regex StreamIdPattern = new(
        @"""strms"":\[(?:\{[^}]*\},)*?\{[^}]*""id"":""([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public StreamingService Service => StreamingService.Espn;

    public async Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawLink))
            return null;

        var gameIdMatch = GameIdPattern.Match(rawLink);
        if (!gameIdMatch.Success)
            return null;

        var gameId = gameIdMatch.Groups[1].Value;

        await using var session = await sessionProvider.CreateSessionAsync(ct);
        var page = await session.Context.NewPageAsync();

        await page.GotoAndWaitForLoadAsync(
            $"https://www.espn.com/watch/player/_/eventCalendarId/{gameId}", ct);

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
