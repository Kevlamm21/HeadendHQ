using HeadendHQ.Core.Settings;

namespace HeadendHQ.WebScraping.Transport;

internal sealed record TransportProfile(
    string Name,
    IReadOnlyList<string> Hosts,
    int RequestsPerMinute,
    int? RequestsPerHour,
    int MinDelayMs,
    int JitterMs,
    int MaxConcurrency,
    IReadOnlyDictionary<string, string>? Headers = null,
    Func<string, string?>? FallbackHost = null,
    int MaxAttempts = 3)
{
    public static readonly TransportProfile Default = new(
        "default",
        Hosts: [],
        RequestsPerMinute: 30,
        RequestsPerHour: 500,
        MinDelayMs: 1000,
        JitterMs: 500,
        MaxConcurrency: 1,
        Headers: new Dictionary<string, string>
        {
            ["User-Agent"] = SourceSettings.UserAgent,
            ["Accept"] = "*/*",
            ["Accept-Language"] = "en-US,en;q=0.9",
        });

    public int Match(string host) =>
        Hosts
            .Where(h => host.Equals(h, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase))
            .Select(h => h.Length)
            .DefaultIfEmpty(0)
            .Max();
}
