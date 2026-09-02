using System.Text.Json.Serialization;

namespace HeadendHQ.WebScraping.Espn.Models;

internal record EspnFeedRoot(
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("events")] List<EspnEvent>? Events
);

internal record EspnEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("sport")] EspnSportRef Sport,
    [property: JsonPropertyName("league")] EspnLeagueRef League,
    [property: JsonPropertyName("competitors")] List<EspnCompetitor> Competitors,
    [property: JsonPropertyName("watch")] EspnWatch? Watch,
    [property: JsonPropertyName("season")] EspnFeedSeason? Season
);

internal record EspnFeedSeason(
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("type")] int? Type,
    [property: JsonPropertyName("slug")] string? Slug
);

internal record EspnSportRef(
    [property: JsonPropertyName("slug")] string Slug
);

internal record EspnLeagueRef(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("id")] string? Id
);

internal record EspnCompetitor(
    [property: JsonPropertyName("homeAway")] string? HomeAway,
    [property: JsonPropertyName("team")] EspnTeam Team
);

internal record EspnTeam(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("color")] string? Color,
    [property: JsonPropertyName("alternateColor")] string? AlternateColor,
    [property: JsonPropertyName("logos")] List<EspnLogo>? Logos
);

internal record EspnWatch(
    [property: JsonPropertyName("style")] EspnWatchStyle? Style,
    [property: JsonPropertyName("broadcasts")] List<EspnBroadcast>? Broadcasts
);

internal record EspnWatchStyle(
    [property: JsonPropertyName("action")] string? Action,
    [property: JsonPropertyName("link")] string? Link
);

internal record EspnBroadcast(
    [property: JsonPropertyName("media")] EspnMedia Media,
    [property: JsonPropertyName("type")] EspnBroadcastType? Type,
    [property: JsonPropertyName("market")] EspnBroadcastMarket? Market,
    [property: JsonPropertyName("priority")] int? Priority
);

/// <summary>"television" or "streaming" — the broadcaster's kind, for free, per airing.</summary>
internal record EspnBroadcastType(
    [property: JsonPropertyName("slug")] string? Slug
);

internal record EspnBroadcastMarket(
    [property: JsonPropertyName("type")] string? Type
);

internal record EspnMedia(
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("callLetters")] string? CallLetters
);
