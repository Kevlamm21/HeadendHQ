using System.Text.Json;
using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Peacock;

public class PeacockExtractor(PeacockLinkResolver linkResolver) : IAdbExtractor
{
    private static readonly Regex IdPattern = new(
        @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public StreamingService Service => StreamingService.Peacock;

    public async Task<string?> BuildCommandAsync(string? eventUrl, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(eventUrl))
        {
            var command = TryBuildCommand(eventUrl);
            if (command is not null)
                return command;
        }

        var resolvedUrl = await linkResolver.ResolveAsync(eventUrl, ct);
        return resolvedUrl is not null ? TryBuildCommand(resolvedUrl) : null;
    }

    private static string? TryBuildCommand(string url)
    {
        var match = IdPattern.Match(url);
        if (!match.Success)
            return null;

        var id = match.Groups[1].Value;
        var deeplinkData = JsonSerializer.Serialize(new { pvid = id, type = "PROGRAMME", action = "PLAY" });
        var encoded = Uri.EscapeDataString(deeplinkData);
        return $"am start -a android.intent.action.VIEW " +
               $"-d 'https://www.peacocktv.com/deeplink?deeplinkData={encoded}'";
    }
}
