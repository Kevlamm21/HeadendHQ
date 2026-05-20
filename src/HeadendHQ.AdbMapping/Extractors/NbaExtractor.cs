using System.Text.RegularExpressions;
using HeadendHQ.Core.SportingEvents;

namespace HeadendHQ.AdbMapping.Extractors;

public class NbaExtractor : IAdbExtractor
{
    private static readonly Regex IdPattern = new(@"([0-9]{10})$", RegexOptions.Compiled);

    public StreamingService Service => StreamingService.NbaLeaguePass;

    public string BuildCommand(string eventUrl)
    {
        var match = IdPattern.Match(eventUrl);
        if (!match.Success)
            throw new ArgumentException($"Could not extract game ID from NBA URL: {eventUrl}");

        var id = match.Groups[1].Value;
        return $"am start -a android.intent.action.VIEW -d \"gametime://game/{id}\" com.nbaimd.gametime.nba2011";
    }
}
