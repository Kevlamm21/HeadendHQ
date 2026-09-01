namespace HeadendHQ.Espn.Transport;

/// <summary>
/// Every ESPN URL we build, in one place.
/// <para>
/// ESPN spreads the same data across three hosts with different conventions, and the choice of host
/// is not cosmetic: <c>site.api.espn.com</c> sits behind Akamai bot management, while
/// <c>site.web.api.espn.com</c> mirrors the same paths more leniently and is the fallback when the
/// former starts refusing us.
/// </para>
/// </summary>
internal static class EspnEndpoints
{
    public const string SiteApi = "https://site.api.espn.com";
    public const string SiteWebApi = "https://site.web.api.espn.com";
    public const string CoreApi = "https://sports.core.api.espn.com";

    /// <summary>
    /// The 17 sports. Returns bare <c>$ref</c>s, but each URL ends in the sport's slug, so the whole
    /// catalog of slugs costs one request.
    /// <para>
    /// Note the deliberate absence of <c>/v2/ontology/leagues</c>: it looks like the natural
    /// cross-sport catalog but returns 2913 unresolved links, where the per-sport lists are both
    /// complete and cheap.
    /// </para>
    /// </summary>
    public static string Sports() => $"{CoreApi}/v2/sports";

    public static string Sport(string sportSlug) => $"{CoreApi}/v2/sports/{sportSlug}";

    public static string Leagues(string sportSlug, int limit = 1000) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues?limit={limit}";

    public static string League(string sportSlug, string leagueSlug) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues/{leagueSlug}";

    /// <summary>
    /// Every team in a league with colours and all sixteen logo variants, in one request.
    /// The high limit matters — NCAA silently truncates at the default and returns a partial league.
    /// </summary>
    public static string Teams(string sportSlug, string leagueSlug, int limit = 1000) =>
        $"{SiteApi}/apis/site/v2/sports/{sportSlug}/{leagueSlug}/teams?limit={limit}";

    /// <summary>
    /// A single team's record on the core API.
    /// <para>
    /// This exists because the bulk <see cref="Teams"/> listing cannot be trusted for the
    /// <c>guid</c>-addressed logo variants: for the NFL it returns each team the <em>previous</em>
    /// team id's guid, so every on-colour logo comes back as the wrong club. Verified across all 32
    /// NFL teams, and verified absent for the NBA. This endpoint returns the correct guid.
    /// </para>
    /// </summary>
    public static string Team(string sportSlug, string leagueSlug, string teamId) =>
        $"{CoreApi}/v2/sports/{sportSlug}/leagues/{leagueSlug}/teams/{teamId}";

    /// <summary>Carries the league's own logos and the current season block.</summary>
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

    /// <summary>
    /// A broadcaster record with its logo URLs. Media ids are global — the same record comes back
    /// through any sport/league path — so the path segments here are just a required prefix.
    /// </summary>
    public static string Media(string mediaId) =>
        $"{CoreApi}/v2/sports/basketball/leagues/nba/media/{mediaId}";

    /// <summary>
    /// The media index. Media ids are global, so the <c>basketball/nba</c> path segments are a
    /// required-but-arbitrary prefix (as with <see cref="Media"/>) — this returns every network ESPN
    /// knows, ~1309 of them, not NBA ones. Each item is a bare <c>$ref</c> whose trailing segment is
    /// the numeric media id; there is no slug or name inline.
    /// </summary>
    public static string MediaIndex(int page, int limit = 1000) =>
        $"{CoreApi}/v2/sports/basketball/leagues/nba/media?limit={limit}&page={page}";

    /// <summary>
    /// The watch guide: the only feed that says where an event can actually be streamed, which is
    /// the whole reason we prefer it over the plain scoreboard.
    /// </summary>
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

    /// <summary>
    /// Swaps a blocked <c>site.api</c> URL for its <c>site.web.api</c> twin, which serves the same
    /// <c>/apis/site/v2/...</c> tree.
    /// </summary>
    public static string? FallbackHost(string url) =>
        url.StartsWith($"{SiteApi}/apis/site/", StringComparison.Ordinal)
            ? string.Concat(SiteWebApi, url.AsSpan(SiteApi.Length))
            : null;

    /// <summary>
    /// Core-API <c>$ref</c> values sometimes leak ESPN's internal hostname and always come back as
    /// plain http. Both have to be corrected before the link can be followed.
    /// </summary>
    public static string NormalizeRef(string href) =>
        href.Replace("sports.core.api.espn.pvt", "sports.core.api.espn.com", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
}
