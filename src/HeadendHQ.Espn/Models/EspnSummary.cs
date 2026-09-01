using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeadendHQ.Espn.Models;

internal record EspnSummaryRoot(
    [property: JsonPropertyName("header")] EspnSummaryHeader? Header,
    [property: JsonPropertyName("gameInfo")] EspnGameInfo? GameInfo,
    [property: JsonPropertyName("boxscore")] EspnBoxscore? Boxscore,
    [property: JsonPropertyName("rosters")] List<EspnRosterTeam>? Rosters,
    [property: JsonPropertyName("leaders")] List<EspnLeadersTeam>? Leaders
);

internal record EspnSummaryTeamRef(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("displayName")] string? DisplayName
);

internal record EspnBoxscore(
    [property: JsonPropertyName("players")] List<EspnBoxscoreTeamPlayers>? Players
);

internal record EspnBoxscoreTeamPlayers(
    [property: JsonPropertyName("team")] EspnSummaryTeamRef? Team,
    [property: JsonPropertyName("statistics")] List<EspnBoxscoreStatistic>? Statistics
);

internal record EspnBoxscoreStatistic(
    [property: JsonPropertyName("athletes")] List<EspnBoxscoreAthleteEntry>? Athletes
);

internal record EspnBoxscoreAthleteEntry(
    [property: JsonPropertyName("athlete")] EspnAthlete? Athlete
);

// Starting lineups / probable pitchers. Present for scheduled games in some leagues (MLB today).
internal record EspnRosterTeam(
    [property: JsonPropertyName("team")] EspnSummaryTeamRef? Team,
    [property: JsonPropertyName("homeAway")] string? HomeAway,
    [property: JsonPropertyName("roster")] List<EspnRosterEntry>? Roster
);

internal record EspnRosterEntry(
    [property: JsonPropertyName("athlete")] EspnAthlete? Athlete,
    [property: JsonPropertyName("starter")] bool? Starter
);

// Season stat leaders. Populated once teams have played games this season, so it is
// empty during preseason and week 1.
internal record EspnLeadersTeam(
    [property: JsonPropertyName("team")] EspnSummaryTeamRef? Team,
    [property: JsonPropertyName("leaders")] List<EspnLeaderCategory>? Leaders
);

internal record EspnLeaderCategory(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("leaders")] List<EspnLeaderEntry>? Leaders
);

internal record EspnLeaderEntry(
    [property: JsonPropertyName("athlete")] EspnAthlete? Athlete
);

internal record EspnAthlete(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("shortName")] string? ShortName,
    [property: JsonPropertyName("headshot")] EspnHeadshot? Headshot,
    [property: JsonPropertyName("position")] EspnAthletePosition? Position,
    [property: JsonPropertyName("jersey")] string? Jersey,
    [property: JsonPropertyName("experience")] EspnExperience? Experience,
    [property: JsonPropertyName("injuries"), JsonConverter(typeof(EspnInjuriesConverter))] List<EspnInjury>? Injuries,
    [property: JsonPropertyName("status")] EspnAthleteStatus? Status
);

internal record EspnInjury(
    [property: JsonPropertyName("status")] string? Status
);

internal sealed class EspnInjuriesConverter : JsonConverter<List<EspnInjury>>
{
    public override List<EspnInjury>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.StartArray)
            return JsonSerializer.Deserialize<List<EspnInjury>>(ref reader, options);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var status = document.RootElement.TryGetProperty("status", out var statusProperty)
                ? statusProperty.GetString()
                : null;
            return [new EspnInjury(status)];
        }

        if (reader.TokenType == JsonTokenType.String)
            return [new EspnInjury(reader.GetString())];

        reader.Skip();
        return null;
    }

    public override void Write(
        Utf8JsonWriter writer, List<EspnInjury> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}

[JsonConverter(typeof(EspnAthleteStatusConverter))]
internal record EspnAthleteStatus(
    [property: JsonPropertyName("type")] string? Type
);

internal sealed class EspnAthleteStatusConverter : JsonConverter<EspnAthleteStatus>
{
    public override EspnAthleteStatus? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
            return new EspnAthleteStatus(reader.GetString());

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var type = document.RootElement.TryGetProperty("type", out var typeProperty)
                ? typeProperty.GetString()
                : null;
            return new EspnAthleteStatus(type);
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, EspnAthleteStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Type);
}

internal record EspnExperience(
    [property: JsonPropertyName("years")] int? Years
);

internal record EspnHeadshot(
    [property: JsonPropertyName("href")] string? Href
);

internal record EspnAthletePosition(
    [property: JsonPropertyName("abbreviation")] string? Abbreviation,
    [property: JsonPropertyName("displayName")] string? DisplayName
);

internal record EspnSummaryHeader(
    [property: JsonPropertyName("gameNote")] string? GameNote,
    [property: JsonPropertyName("competitions")] List<EspnSummaryCompetition>? Competitions
);

internal record EspnSummaryCompetition(
    [property: JsonPropertyName("series")] List<EspnSeries>? Series,
    [property: JsonPropertyName("competitors")] List<EspnSummaryCompetitor>? Competitors
);

internal record EspnSummaryCompetitor(
    [property: JsonPropertyName("homeAway")] string? HomeAway,
    [property: JsonPropertyName("team")] EspnSummaryTeamRef? Team,
    [property: JsonPropertyName("probables")] List<EspnProbable>? Probables
);

// The probable starting pitchers for this specific game (MLB).
internal record EspnProbable(
    [property: JsonPropertyName("athlete")] EspnAthlete? Athlete
);

internal record EspnSeries(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("summary")] string? Summary
);

internal record EspnGameInfo(
    [property: JsonPropertyName("venue")] EspnVenue? Venue
);

internal record EspnVenue(
    [property: JsonPropertyName("fullName")] string? FullName
);
