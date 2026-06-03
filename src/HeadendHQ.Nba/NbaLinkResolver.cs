using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Nba;

public class NbaLinkResolver : ILinkResolver
{
    private static readonly Regex IdPattern = new(@"([0-9]{10})", RegexOptions.Compiled);

    public StreamingService Service => StreamingService.NbaLeaguePass;

    public Task<string?> ResolveAsync(string? rawLink, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rawLink))
            return Task.FromResult<string?>(null);

        var match = IdPattern.Match(rawLink);
        return Task.FromResult(match.Success
            ? $"https://www.nba.com/game/{match.Groups[1].Value}"
            : (string?)null);
    }
}
