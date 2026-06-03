using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Nba;

public class NbaExtractor(NbaLinkResolver linkResolver) : IAdbExtractor
{
    private static readonly Regex IdPattern = new(@"([0-9]{10})$", RegexOptions.Compiled);

    public StreamingService Service => StreamingService.NbaLeaguePass;

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
        return $"am start -a android.intent.action.VIEW -d \"gametime://game/{id}\" com.nbaimd.gametime.nba2011";
    }
}
