namespace HeadendHQ.WebScraping.Espn.Transport;

internal static class EspnEndpoints
{
    public const string SiteApi = "https://site.api.espn.com";
    public const string SiteWebApi = "https://site.web.api.espn.com";
    public const string CoreApi = "https://sports.core.api.espn.com";

    public static string Sports() => $"{CoreApi}/v2/sports";

    public static string Sport(string sportSlug) => $"{CoreApi}/v2/sports/{sportSlug}";

    public static string Leagues(string sportSlug, int limit = 1000) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues?limit={limit}";

    public static string League(string sportSlug, string leagueSlug) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues/{leagueSlug}";

    public static string Teams(string sportSlug, string leagueSlug, int limit = 1000) =>
        $"{SiteApi}/apis/site/v2/sports/{sportSlug}/{leagueSlug}/teams?limit={limit}";

    public static string Team(string sportSlug, string leagueSlug, string teamId) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues/{leagueSlug}/teams/{teamId}";

    public static string Scoreboard(string sportSlug, string leagueSlug) =>
        $"{SiteApi}/apis/site/v2/sports/{sportSlug}/{leagueSlug}/scoreboard";

    public static string ScoreboardForDate(string sportSlug, string leagueSlug, string yyyyMMdd) =>
        $"{SiteApi}/apis/site/v2/sports/{sportSlug}/{leagueSlug}/scoreboard?dates={yyyyMMdd}";

    public static string Summary(string sportSlug, string leagueSlug, string eventId) =>
        $"{SiteApi}/apis/site/v2/sports/{sportSlug}/{leagueSlug}/summary?event={eventId}";

    public static string Roster(string sportSlug, string leagueSlug, string teamId) =>
        $"{SiteApi}/apis/site/v2/sports/{sportSlug}/{leagueSlug}/teams/{teamId}/roster";

    public static string DepthChart(string sportSlug, string leagueSlug, string teamId, int seasonYear) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues/{leagueSlug}/seasons/{seasonYear}/teams/{teamId}/depthcharts";

    public static string Media(string mediaId) =>
        $"{CoreApi}/v2/sports/basketball/leagues/nba/media/{mediaId}";

    public static string MediaIndex(int page, int limit = 1000) =>
        $"{CoreApi}/v2/sports/basketball/leagues/nba/media?limit={limit}&page={page}";

    public static string GuideFeed(string yyyyMMdd, int page, int limit, string? leagues, string? watch)
    {
        var url = $"{SiteWebApi}/apis/personalized/site/v2/guide/feed" +
                  "?region=us&lang=en&configuration=STREAM_MENU&platform=web&buyWindow=1m" +
                  "&showAirings=buy%2Clive&ontology=true&hydrated=favorites&playabilitySource=playbackId" +
                  $"&tz=America%2FNew_York&dates={yyyyMMdd}&page={page}&limit={limit}";

        if (!string.IsNullOrEmpty(leagues))
            url += $"&leagues={Uri.EscapeDataString(leagues)}";

        if (!string.IsNullOrEmpty(watch))
            url += $"&watch={Uri.EscapeDataString(watch)}";

        return url;
    }

    public static string? FallbackHost(string url) =>
        url.StartsWith($"{SiteApi}/apis/site/", StringComparison.Ordinal)
            ? string.Concat(SiteWebApi, url.AsSpan(SiteApi.Length))
            : null;

    public static string NormalizeRef(string href) =>
        href.Replace("sports.core.api.espn.pvt", "sports.core.api.espn.com", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
}
