using HeadendHQ.Core.Settings;
using HeadendHQ.WebScraping.Transport;

namespace HeadendHQ.WebScraping.Espn.Transport;

internal static class EspnProfiles
{
    // ESPN's APIs are not public and answer normally only for something that looks like espn.com.
    private static readonly Dictionary<string, string> BrowserHeaders = new()
    {
        ["User-Agent"] = SourceSettings.UserAgent,
        ["Accept"] = "*/*",
        ["Accept-Language"] = "en-US,en;q=0.9",
        ["DNT"] = "1",
        ["Origin"] = "https://www.espn.com",
        ["Referer"] = "https://www.espn.com/",
        ["sec-ch-ua"] = "\"Chromium\";v=\"148\", \"Google Chrome\";v=\"148\", \"Not/A)Brand\";v=\"99\"",
        ["sec-ch-ua-mobile"] = "?0",
        ["sec-ch-ua-platform"] = "\"Windows\"",
        ["sec-fetch-dest"] = "empty",
        ["sec-fetch-mode"] = "cors",
        ["sec-fetch-site"] = "same-site",
    };

    // The CDN is a plain file host: it serves artwork identically without the espn.com referer, and
    // there is no reason to send one to it.
    private static readonly Dictionary<string, string> CdnHeaders = new()
    {
        ["User-Agent"] = SourceSettings.UserAgent,
        ["Accept"] = "image/avif,image/webp,image/png,image/*;q=0.8,*/*;q=0.5",
        ["Accept-Language"] = "en-US,en;q=0.9",
    };

    public static readonly TransportProfile Api = new(
        "espn-api",
        Hosts: ["site.api.espn.com", "site.web.api.espn.com", "sports.core.api.espn.com"],
        RequestsPerMinute: 60,
        RequestsPerHour: 1500,
        MinDelayMs: 400,
        JitterMs: 400,
        MaxConcurrency: 2,
        Headers: BrowserHeaders,
        FallbackHost: EspnEndpoints.FallbackHost);

    public static readonly TransportProfile Cdn = new(
        "espn-cdn",
        Hosts: ["espncdn.com"],
        RequestsPerMinute: 300,
        RequestsPerHour: null,
        MinDelayMs: 100,
        JitterMs: 100,
        MaxConcurrency: 4,
        Headers: CdnHeaders);

    // Loading a watch page drives a real browser, so it is paced far more gently than a JSON call
    // and kept off the API's budget entirely.
    public static readonly TransportProfile Watch = new(
        "espn-watch",
        Hosts: ["espn.com"],
        RequestsPerMinute: 20,
        RequestsPerHour: 300,
        MinDelayMs: 1500,
        JitterMs: 1000,
        MaxConcurrency: 1);

    public static IEnumerable<TransportProfile> All => [Api, Cdn, Watch];
}
