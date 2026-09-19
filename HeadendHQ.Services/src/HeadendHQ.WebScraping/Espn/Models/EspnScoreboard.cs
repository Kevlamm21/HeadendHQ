using System.Text.Json.Serialization;

namespace HeadendHQ.WebScraping.Espn.Models;

internal record EspnScoreboardRoot(
    [property: JsonPropertyName("events")] List<EspnScoreboardEvent>? Events
);

internal record EspnScoreboardEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("season")] EspnScoreboardSeason? Season,
    [property: JsonPropertyName("competitions")] List<EspnCompetition>? Competitions
);

internal record EspnScoreboardSeason(
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("type")] int? Type
);

internal record EspnCompetition(
    [property: JsonPropertyName("venue")] EspnCompetitionVenue? Venue,
    [property: JsonPropertyName("notes")] List<EspnCompetitionNote>? Notes,
    [property: JsonPropertyName("series")] EspnCompetitionSeries? Series,
    [property: JsonPropertyName("competitors")] List<EspnScoreboardCompetitor>? Competitors
);

internal record EspnCompetitionVenue(
    [property: JsonPropertyName("fullName")] string? FullName
);

internal record EspnCompetitionNote(
    [property: JsonPropertyName("headline")] string? Headline
);

internal record EspnCompetitionSeries(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("summary")] string? Summary
);

internal record EspnScoreboardCompetitor(
    [property: JsonPropertyName("homeAway")] string? HomeAway,
    [property: JsonPropertyName("team")] EspnScoreboardTeamRef? Team,
    [property: JsonPropertyName("leaders")] List<EspnLeaderCategory>? Leaders
);

internal record EspnScoreboardTeamRef(
    [property: JsonPropertyName("id")] string? Id
);

internal record EspnLeaderCategory(
    [property: JsonPropertyName("leaders")] List<EspnLeaderEntry>? Leaders
);

internal record EspnLeaderEntry(
    [property: JsonPropertyName("athlete")] EspnLeaderAthlete? Athlete
);

internal record EspnLeaderAthlete(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("headshot")] string? Headshot,
    [property: JsonPropertyName("position")] EspnLeaderPosition? Position
);

internal record EspnLeaderPosition(
    [property: JsonPropertyName("abbreviation")] string? Abbreviation
);
