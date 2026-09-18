using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using HeadendHQ.WebScraping.Espn.Transport;

namespace HeadendHQ.WebScraping.Espn;

internal static partial class EspnDepthChart
{
    [GeneratedRegex(@"/athletes/(\d+)", RegexOptions.Compiled)]
    private static partial Regex AthleteIdPattern { get; }

    public static async Task<IReadOnlyDictionary<string, int>> FetchAsync(
        EspnTransport transport,
        string sportSlug,
        string leagueSlug,
        string teamId,
        int seasonYear,
        ILogger logger,
        CancellationToken ct)
    {
        foreach (var season in new[] { seasonYear, seasonYear - 1 })
        {
            var ranks = await TryFetchAsync(transport, sportSlug, leagueSlug, teamId, season, logger, ct);
            if (ranks.Count > 0)
                return ranks;
        }

        return new Dictionary<string, int>();
    }

    private static async Task<Dictionary<string, int>> TryFetchAsync(
        EspnTransport transport,
        string sportSlug,
        string leagueSlug,
        string teamId,
        int season,
        ILogger logger,
        CancellationToken ct)
    {
        var ranks = new Dictionary<string, int>();

        try
        {
            var url = EspnEndpoints.DepthChart(sportSlug, leagueSlug, teamId, season);
            var json = await transport.GetStringAsync(url, ct);

            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return ranks;

            foreach (var chart in items.EnumerateArray())
            {
                if (!chart.TryGetProperty("positions", out var positions) ||
                    positions.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var position in positions.EnumerateObject())
                    CollectRanks(position.Value, ranks);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not EspnThrottledException)
        {
            logger.LogDebug(ex, "No {League} depth chart for team {TeamId} in season {Season}.", leagueSlug, teamId, season);
        }

        return ranks;
    }

    private static void CollectRanks(JsonElement position, Dictionary<string, int> ranks)
    {
        if (!position.TryGetProperty("athletes", out var athletes) ||
            athletes.ValueKind != JsonValueKind.Array)
            return;

        foreach (var entry in athletes.EnumerateArray())
        {
            if (!entry.TryGetProperty("athlete", out var athlete) ||
                !athlete.TryGetProperty("$ref", out var reference) ||
                reference.GetString() is not { } href)
                continue;

            var match = AthleteIdPattern.Match(href);
            if (!match.Success)
                continue;

            var rank = entry.TryGetProperty("rank", out var rankElement) &&
                       rankElement.TryGetInt32(out var parsed)
                ? parsed
                : int.MaxValue;

            var id = match.Groups[1].Value;

            if (!ranks.TryGetValue(id, out var existing) || rank < existing)
                ranks[id] = rank;
        }
    }
}
