using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeadendHQ.WebScraping.Espn.Models;

internal static class EspnRoster
{
    private static readonly HashSet<string> InactiveGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "injuredReserveOrOut",
        "suspended",
        "practiceSquad",
    };

    public static List<EspnAthlete> FlattenActiveAthletes(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("athletes", out var athletes) ||
            athletes.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<EspnAthlete>();

        foreach (var element in athletes.EnumerateArray())
        {
            if (element.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                if (IsInactiveGroup(element))
                    continue;

                foreach (var item in items.EnumerateArray())
                    Append(results, item);

                continue;
            }

            Append(results, element);
        }

        return results;
    }

    private static bool IsInactiveGroup(JsonElement group) =>
        group.TryGetProperty("position", out var position) &&
        position.ValueKind == JsonValueKind.String &&
        position.GetString() is { } name &&
        InactiveGroups.Contains(name);

    private static void Append(List<EspnAthlete> results, JsonElement element)
    {
        try
        {
            if (element.Deserialize<EspnAthlete>() is { Id.Length: > 0 } athlete)
                results.Add(athlete);
        }
        catch (JsonException)
        {
        }
    }
}

internal record EspnAthlete(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("headshot")] EspnHeadshot? Headshot,
    [property: JsonPropertyName("position")] EspnAthletePosition? Position,
    [property: JsonPropertyName("experience")] EspnExperience? Experience,
    [property: JsonPropertyName("injuries"), JsonConverter(typeof(EspnInjuriesConverter))] List<EspnInjury>? Injuries,
    [property: JsonPropertyName("status")] EspnAthleteStatus? Status
);

internal record EspnHeadshot(
    [property: JsonPropertyName("href")] string? Href
);

internal record EspnAthletePosition(
    [property: JsonPropertyName("abbreviation")] string? Abbreviation,
    [property: JsonPropertyName("displayName")] string? DisplayName
);

internal record EspnExperience(
    [property: JsonPropertyName("years")] int? Years
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
