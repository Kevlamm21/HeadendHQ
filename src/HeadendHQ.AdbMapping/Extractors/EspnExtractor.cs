using System.Text.RegularExpressions;
using HeadendHQ.Core.SportingEvents;

namespace HeadendHQ.AdbMapping.Extractors;

public class EspnExtractor : IAdbExtractor
{
    private static readonly Regex UuidPattern = new(
        @"\/id\/([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public StreamingService Service => StreamingService.Espn;

    public string BuildCommand(string eventUrl)
    {
        var uuidMatch = UuidPattern.Match(eventUrl);
        if (uuidMatch.Success)
        {
            var id = uuidMatch.Groups[1].Value;
            return $"am start -a android.intent.action.VIEW " +
                   $"-d \"sportscenter://x-callback-url/showWatchStream?playID={id}&x-source=recommendation\" " +
                   $"-n com.espn.score_center/com.espn.startup.presentation.StartupActivity";
        }

        throw new ArgumentException($"Could not extract playback ID from ESPN URL: {eventUrl}");
    }
}
