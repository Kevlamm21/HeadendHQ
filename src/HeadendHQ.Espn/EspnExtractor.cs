using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Espn;

public class EspnExtractor(EspnLinkResolver linkResolver) : IAdbExtractor
{
    private static readonly Regex UuidPattern = new(
        @"\/id\/([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public StreamingService Service => StreamingService.Espn;

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
        var match = UuidPattern.Match(url);
        if (!match.Success)
            return null;

        var id = match.Groups[1].Value;
        return $"am start -a android.intent.action.VIEW " +
               $"-d \"sportscenter://x-callback-url/showWatchStream?playID={id}&x-source=recommendation\" " +
               $"-n com.espn.score_center/com.espn.startup.presentation.StartupActivity";
    }
}
