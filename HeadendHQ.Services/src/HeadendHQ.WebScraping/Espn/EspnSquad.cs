using System.Text.Json;
using System.Text.RegularExpressions;
using HeadendHQ.Core;
using HeadendHQ.Core.Events;
using HeadendHQ.WebScraping.Espn.Models;
using HeadendHQ.WebScraping.Espn.Transport;
using HeadendHQ.WebScraping.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn;

internal static partial class EspnSquad
{
    [GeneratedRegex(@"/athletes/(\d+)", RegexOptions.Compiled)]
    private static partial Regex AthleteIdPattern { get; }

    public static async Task<IReadOnlyList<AthleteRequest>> FetchAsync(
        WebTransport transport,
        string sportSlug,
        string leagueSlug,
        string teamId,
        int seasonYear,
        ILogger logger,
        CancellationToken ct)
    {
        var roster = await FetchRosterAsync(transport, sportSlug, leagueSlug, teamId, logger, ct);

        if (roster.Count == 0)
            return roster;

        var ranks = await FetchRanksAsync(transport, sportSlug, leagueSlug, teamId, seasonYear, logger, ct);

        return ranks.Count == 0
            ? roster
            : [.. roster.Select(a => ranks.TryGetValue(a.ExternalId, out var rank)
                ? a with { DepthRank = rank }
                : a)];
    }

    private static async Task<IReadOnlyList<AthleteRequest>> FetchRosterAsync(
        WebTransport transport,
        string sportSlug,
        string leagueSlug,
        string teamId,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            var json = await transport.GetStringAsync(
                EspnEndpoints.Roster(sportSlug, leagueSlug, teamId), ct);

            return [.. EspnRoster.FlattenActiveAthletes(json)
                .Where(a => !string.IsNullOrEmpty(a.Id))
                .Select(a => new AthleteRequest(
                    ExternalId: a.Id,
                    DisplayName: a.DisplayName ?? "Unknown",
                    Position: a.Position?.Abbreviation ?? a.Position?.DisplayName,
                    ExperienceYears: a.Experience?.Years,
                    HeadshotUrl: a.Headshot?.Href,
                    InjuryStatus: a.Injuries?.FirstOrDefault()?.Status ?? a.Status?.Type))];
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not CatalogSourceThrottledException)
        {
            logger.LogWarning(ex, "Failed to fetch the {League} roster for team {TeamId}.", leagueSlug, teamId);
            return [];
        }
    }
    
    private static async Task<Dictionary<string, int>> FetchRanksAsync(
        WebTransport transport,
        string sportSlug,
        string leagueSlug,
        string teamId,
        int seasonYear,
        ILogger logger,
        CancellationToken ct)
    {
        foreach (var season in new[] { seasonYear, seasonYear - 1 })
        {
            var ranks = await TryFetchRanksAsync(transport, sportSlug, leagueSlug, teamId, season, logger, ct);
            if (ranks.Count > 0)
                return ranks;
        }

        return [];
    }

    private static async Task<Dictionary<string, int>> TryFetchRanksAsync(
        WebTransport transport,
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
            var json = await transport.GetStringAsync(
                EspnEndpoints.DepthChart(sportSlug, leagueSlug, teamId, season), ct);

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
        catch (Exception ex) when (ex is not OperationCanceledException and not CatalogSourceThrottledException)
        {
            logger.LogDebug(ex, "No {League} depth chart for team {TeamId} in season {Season}.",
                leagueSlug, teamId, season);
        }

        return ranks;
    }

    // A team fields several formations, so an athlete can appear more than once; their best rank wins.
    // The athlete is a $ref rather than an inline record, so the id comes out of the href.
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
