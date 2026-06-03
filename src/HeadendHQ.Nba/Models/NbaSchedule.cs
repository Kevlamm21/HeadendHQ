using System.Text.Json.Serialization;

namespace HeadendHQ.Nba.Models;

internal record NbaScheduleRoot(
    [property: JsonPropertyName("leagueSchedule")] NbaLeagueSchedule LeagueSchedule
);

internal record NbaLeagueSchedule(
    [property: JsonPropertyName("gameDates")] List<NbaGameDate> GameDates
);

internal record NbaGameDate(
    [property: JsonPropertyName("gameDate")] string GameDate,
    [property: JsonPropertyName("games")] List<NbaGame> Games
);

internal record NbaGame(
    [property: JsonPropertyName("gameId")] string GameId,
    [property: JsonPropertyName("gameDateTimeUTC")] string GameDateTimeUtc,
    [property: JsonPropertyName("gameLabel")] string? GameLabel,
    [property: JsonPropertyName("seriesText")] string? SeriesText,
    [property: JsonPropertyName("homeTeam")] NbaTeam HomeTeam,
    [property: JsonPropertyName("awayTeam")] NbaTeam AwayTeam,
    [property: JsonPropertyName("broadcasters")] NbaBroadcasters Broadcasters
);

internal record NbaTeam(
    [property: JsonPropertyName("teamCity")] string TeamCity,
    [property: JsonPropertyName("teamName")] string TeamName
);

internal record NbaBroadcasters(
    [property: JsonPropertyName("nationalTvBroadcasters")] List<NbaBroadcaster> NationalBroadcasters
);

internal record NbaBroadcaster(
    [property: JsonPropertyName("broadcasterDisplay")] string BroadcasterDisplay,
    [property: JsonPropertyName("broadcasterVideoLink")] string? VideoLink
);
