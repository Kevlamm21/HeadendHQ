using System.Text.Json;
using System.Text.RegularExpressions;
using HeadendHQ.Core.SportingEvents;

namespace HeadendHQ.AdbMapping.Extractors;

public class PeacockExtractor : IAdbExtractor
{
    private static readonly Regex IdPattern = new(
        @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public StreamingService Service => StreamingService.Peacock;

    public string BuildCommand(string eventUrl)
    {
        var match = IdPattern.Match(eventUrl);
        if (!match.Success)
            throw new ArgumentException($"Could not extract content ID from Peacock URL: {eventUrl}");

        var id = match.Groups[1].Value;
        var deeplinkData = JsonSerializer.Serialize(new { pvid = id, type = "PROGRAMME", action = "PLAY" });
        var encoded = Uri.EscapeDataString(deeplinkData);
        return $"am start -a android.intent.action.VIEW " +
               $"-d 'https://www.peacocktv.com/deeplink?deeplinkData={encoded}'";
    }
}
