using System.Text.Json;

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
