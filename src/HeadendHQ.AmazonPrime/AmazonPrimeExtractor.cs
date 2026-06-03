using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.AmazonPrime;

public class AmazonPrimeExtractor(AmazonPrimeLinkResolver linkResolver) : IAdbExtractor
{
    private static readonly Regex IdPattern = new(@"\/detail\/([A-Za-z0-9.\-]+)", RegexOptions.Compiled);

    public StreamingService Service => StreamingService.AmazonPrime;

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

        var asin = match.Groups[1].Value;
        return $"am start -a android.intent.action.VIEW " +
               $"-d 'https://watch.amazon.com/detail?asin={asin}' " +
               $"-n com.amazon.amazonvideo.livingroom/com.amazon.ignition.IgnitionActivity";
    }
}
