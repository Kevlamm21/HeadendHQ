using HeadendHQ.WebScraping.Transport;

namespace HeadendHQ.WebScraping.Peacock;

internal static class PeacockProfiles
{
    // Every resolve drives a browser through a redirect chain, so keep it slow and single-file.
    public static readonly TransportProfile Web = new(
        "peacock-web",
        Hosts: ["peacocktv.com"],
        RequestsPerMinute: 20,
        RequestsPerHour: 300,
        MinDelayMs: 1500,
        JitterMs: 1000,
        MaxConcurrency: 1);

    public static IEnumerable<TransportProfile> All => [Web];
}
