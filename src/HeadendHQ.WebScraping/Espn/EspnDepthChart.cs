using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using HeadendHQ.WebScraping.Espn.Transport;

namespace HeadendHQ.WebScraping.Espn;

/// <summary>
/// Depth charts are the only ESPN source that knows who actually starts. They live on
/// the core API rather than the site API, and reference athletes by URL rather than
/// inlining them — but the athlete id is in the URL, so no per-athlete fetch is needed.
/// Not supported for college football, which returns a 400.
/// </summary>
internal static partial class EspnDepthChart
{
    [GeneratedRegex(@"/athletes/(\d+)", RegexOptions.Compiled)]
    private static partial Regex AthleteIdPattern { get; }

    /// <summary>Athlete id to best (lowest) depth rank. Rank 1 is a starter.</summary>
    public static async Task<IReadOnlyDictionary<string, int>> FetchAsync(
        EspnTransport transport,
        string sportSlug,
        string leagueSlug,
        string teamId,
        int seasonYear,
        ILogger logger,
        CancellationToken ct)
    {
        // NBA and NHL seasons straddle the new year, so a January game belongs to the
        // previous season. Try the event's year, then fall back one.
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
                // "positions" is an object keyed by position slug (lde, nt, rde, ...).
                if (!chart.TryGetProperty("positions", out var positions) ||
                    positions.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var position in positions.EnumerateObject())
                    CollectRanks(position.Value, ranks);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not EspnThrottledException)
        {
            // College football is simply unsupported; anything else is transient.
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

            // A player appears in several charts (offense, special teams); keep the best.
            if (!ranks.TryGetValue(id, out var existing) || rank < existing)
                ranks[id] = rank;
        }
    }
}
