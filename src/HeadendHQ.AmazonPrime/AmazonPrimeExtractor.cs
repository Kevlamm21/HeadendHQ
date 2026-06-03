using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.AmazonPrime;

public class AmazonPrimeExtractor : IAdbExtractor
{
    private static readonly Regex IdPattern = new(@"\/detail\/([A-Za-z0-9.\-]+)", RegexOptions.Compiled);

    public StreamingService Service => StreamingService.AmazonPrime;

    public string BuildCommand(string eventUrl)
    {
        var match = IdPattern.Match(eventUrl);
        if (!match.Success)
            throw new ArgumentException($"Could not extract ASIN from Amazon Prime URL: {eventUrl}");

        var asin = match.Groups[1].Value;
        return $"am start -a android.intent.action.VIEW " +
               $"-d 'https://watch.amazon.com/detail?asin={asin}' " +
               $"-n com.amazon.amazonvideo.livingroom/com.amazon.ignition.IgnitionActivity";
    }
}
