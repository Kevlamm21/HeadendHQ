using System.Text.Json;
using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Espn.Models;
using HeadendHQ.Espn.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Espn.Catalog;

/// <summary>
/// The one extra request per event: venue, game note, series standing and the cast.
/// <para>
/// The cast is the awkward part. ESPN fills <c>boxscore.players</c> only once a game is under way
/// and <c>leaders</c> only once teams have current-season stats, so a game scheduled for next week
/// has neither. Hence the chain of sources ending at the team roster — and the roster is the only
/// branch that costs additional requests, which is why it runs last.
/// </para>
/// </summary>
internal sealed class EspnEventDetailSource(
    EspnTransport transport,
    ILogger<EspnEventDetailSource> logger) : IEventDetailSource
{
    public string SourceKey => SourceKeys.Espn;

    public async Task<EventDetailDescriptor?> GetDetailAsync(EventKey key, CancellationToken ct)
    {
        var summary = await FetchSummaryAsync(key, ct);
        if (summary is null)
            return null;

        var competition = summary.Header?.Competitions?.FirstOrDefault();
        var series = competition?.Series?.FirstOrDefault();

        return new EventDetailDescriptor(
            VenueName: summary.GameInfo?.Venue?.FullName,
            Note: summary.Header?.GameNote,
            SeriesType: series?.Type,
            SeriesSummary: series?.Summary,
            Cast: BuildCast(summary));
    }

    /// <summary>
    /// Cast candidates from whichever section of the summary is populated, plus probable starters,
    /// which are the most relevant players in a scheduled baseball game.
    /// </summary>
    private static List<CastCandidate> BuildCast(EspnSummaryRoot summary)
    {
        var candidates = FromRosters(summary);

        if (candidates.Count == 0)
            candidates = FromLeaders(summary);

        if (candidates.Count == 0)
            candidates = FromBoxscore(summary);

        MergeProbables(summary, candidates);
        return candidates;
    }

    private static List<CastCandidate> FromRosters(EspnSummaryRoot summary)
    {
        var candidates = new List<CastCandidate>();

        foreach (var team in summary.Rosters ?? [])
        {
            var isHome = string.Equals(team.HomeAway, "home", StringComparison.OrdinalIgnoreCase);

            foreach (var entry in team.Roster ?? [])
            {
                if (entry.Athlete is null)
                    continue;

                candidates.Add(new CastCandidate(
                    ToDescriptor(entry.Athlete, team.Team?.Id),
                    isHome,
                    team.Team?.DisplayName ?? string.Empty,
                    team.Team?.Id,
                    IsListedStarter: entry.Starter == true,
                    InjuryStatus: InjuryOf(entry.Athlete)));
            }
        }

        return candidates;
    }

    private static List<CastCandidate> FromLeaders(EspnSummaryRoot summary)
    {
        var candidates = new List<CastCandidate>();

        foreach (var team in summary.Leaders ?? [])
        {
            foreach (var athlete in (team.Leaders ?? [])
                         .SelectMany(category => category.Leaders ?? [])
                         .Select(entry => entry.Athlete)
                         .Where(a => a is not null))
            {
                candidates.Add(new CastCandidate(
                    ToDescriptor(athlete!, team.Team?.Id),
                    IsHomeFor(summary, team.Team?.Id),
                    team.Team?.DisplayName ?? string.Empty,
                    team.Team?.Id,
                    IsStatLeader: true,
                    InjuryStatus: InjuryOf(athlete!)));
            }
        }

        return candidates;
    }

    private static List<CastCandidate> FromBoxscore(EspnSummaryRoot summary)
    {
        var candidates = new List<CastCandidate>();

        foreach (var teamPlayers in summary.Boxscore?.Players ?? [])
        {
            foreach (var athlete in (teamPlayers.Statistics ?? [])
                         .SelectMany(stat => stat.Athletes ?? [])
                         .Select(entry => entry.Athlete)
                         .Where(a => a is not null))
            {
                candidates.Add(new CastCandidate(
                    ToDescriptor(athlete!, teamPlayers.Team?.Id),
                    IsHomeFor(summary, teamPlayers.Team?.Id),
                    teamPlayers.Team?.DisplayName ?? string.Empty,
                    teamPlayers.Team?.Id,
                    InjuryStatus: InjuryOf(athlete!)));
            }
        }

        return candidates;
    }

    private static void MergeProbables(EspnSummaryRoot summary, List<CastCandidate> candidates)
    {
        var competitors = summary.Header?.Competitions?.FirstOrDefault()?.Competitors ?? [];

        foreach (var competitor in competitors)
        {
            var isHome = string.Equals(competitor.HomeAway, "home", StringComparison.OrdinalIgnoreCase);

            foreach (var probable in competitor.Probables ?? [])
            {
                if (probable.Athlete is null)
                    continue;

                var index = candidates.FindIndex(c => c.Athlete.ExternalId == probable.Athlete.Id);

                if (index >= 0)
                    candidates[index] = candidates[index] with { IsProbableStarter = true };
                else
                    candidates.Add(new CastCandidate(
                        ToDescriptor(probable.Athlete, competitor.Team?.Id),
                        isHome,
                        competitor.Team?.DisplayName ?? string.Empty,
                        competitor.Team?.Id,
                        IsProbableStarter: true));
            }
        }
    }

    private static bool IsHomeFor(EspnSummaryRoot summary, string? teamId) =>
        teamId is not null &&
        summary.Header?.Competitions?.FirstOrDefault()?.Competitors?
            .FirstOrDefault(c => c.Team?.Id == teamId)?.HomeAway is { } homeAway &&
        homeAway.Equals("home", StringComparison.OrdinalIgnoreCase);

    private static string? InjuryOf(EspnAthlete athlete) =>
        athlete.Injuries?.FirstOrDefault()?.Status ?? athlete.Status?.Type;

    private static AthleteDescriptor ToDescriptor(EspnAthlete athlete, string? teamExternalId) =>
        new(athlete.Id,
            athlete.DisplayName ?? "Unknown",
            athlete.ShortName,
            athlete.Position?.DisplayName ?? athlete.Position?.Abbreviation,
            athlete.Jersey,
            athlete.Experience?.Years,
            athlete.Headshot?.Href,
            teamExternalId);

    private async Task<EspnSummaryRoot?> FetchSummaryAsync(EventKey key, CancellationToken ct)
    {
        try
        {
            var json = await transport.GetStringAsync(
                EspnEndpoints.Summary(key.SportSlug, key.LeagueSlug, key.EventExternalId), ct);

            return JsonSerializer.Deserialize<EspnSummaryRoot>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not EspnThrottledException)
        {
            // Detail is an enhancement; a title without a venue is still a usable title.
            logger.LogWarning(ex, "Failed to read summary for event {Event}.", key.EventExternalId);
            return null;
        }
    }
}
