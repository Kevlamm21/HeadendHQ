using System.Text.Json;
using HeadendHQ.Core;
using HeadendHQ.Core.Events;
using HeadendHQ.WebScraping.Espn.Models;
using HeadendHQ.WebScraping.Espn.Transport;
using HeadendHQ.WebScraping.Transport;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.WebScraping.Espn.Catalog;

internal sealed class EspnEventDetailSource(
    WebTransport transport,
    ILogger<EspnEventDetailSource> logger) : IEventDetailSource
{
    public async Task<IReadOnlyDictionary<string, EventDetailRequest>> GetDetailsAsync(
        IReadOnlyList<EventKey> keys, CancellationToken ct)
    {
        var details = new Dictionary<string, EventDetailRequest>(StringComparer.Ordinal);

        var squads = new Dictionary<(string, string), IReadOnlyList<AthleteRequest>>();
        var calls = 0;

        foreach (var slate in keys.GroupBy(k => (k.SportSlug, k.LeagueSlug, k.Date)))
        {
            ct.ThrowIfCancellationRequested();

            var (sportSlug, leagueSlug, date) = slate.Key;
            var wanted = slate.Select(k => k.EventExternalId).ToHashSet(StringComparer.Ordinal);

            calls++;

            foreach (var espnEvent in await FetchSlateAsync(sportSlug, leagueSlug, date, ct))
            {
                if (!wanted.Contains(espnEvent.Id))
                    continue;

                var competition = espnEvent.Competitions?.FirstOrDefault();
                if (competition is null)
                    continue;

                var seasonYear = espnEvent.Season?.Year ?? date.Year;

                details[espnEvent.Id] = new EventDetailRequest(
                    VenueName: competition.Venue?.FullName,
                    Note: competition.Notes?
                        .Select(n => n.Headline)
                        .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h)),
                    SeriesType: competition.Series?.Type,
                    SeriesSummary: competition.Series?.Summary,
                    SeasonYear: espnEvent.Season?.Year,
                    SeasonType: espnEvent.Season?.Type,
                    Cast: await BuildCastAsync(sportSlug, leagueSlug, seasonYear, competition, squads, ct));
            }
        }

        logger.LogInformation(
            "ESPN detail: {Found} of {Wanted} event(s) from {Calls} scoreboard call(s) and {Squads} squad read(s).",
            details.Count, keys.Count, calls, squads.Count);

        return details;
    }

    private async Task<List<CastRequest>> BuildCastAsync(
        string sportSlug,
        string leagueSlug,
        int seasonYear,
        EspnCompetition competition,
        Dictionary<(string, string), IReadOnlyList<AthleteRequest>> squads,
        CancellationToken ct)
    {
        var cast = new List<CastRequest>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var competitor in competition.Competitors ?? [])
        {
            var isHome = string.Equals(competitor.HomeAway, "home", StringComparison.OrdinalIgnoreCase);

            var leaders = (competitor.Leaders ?? [])
                .SelectMany(category => category.Leaders ?? [])
                .Select(entry => entry.Athlete)
                .OfType<EspnLeaderAthlete>()
                .ToList();

            var starred = leaders.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);

            if (competitor.Team?.Id is { Length: > 0 } teamId)
                foreach (var athlete in await SquadAsync(sportSlug, leagueSlug, teamId, seasonYear, squads, ct))
                    if (seen.Add(athlete.ExternalId))
                        cast.Add(new CastRequest(
                            athlete, isHome, IsStatLeader: starred.Contains(athlete.ExternalId)));

            foreach (var athlete in leaders)
                if (seen.Add(athlete.Id))
                    cast.Add(new CastRequest(
                        new AthleteRequest(
                            ExternalId: athlete.Id,
                            DisplayName: athlete.DisplayName ?? "Unknown",
                            Position: athlete.Position?.Abbreviation,
                            HeadshotUrl: athlete.Headshot),
                        isHome,
                        IsStatLeader: true));
        }

        return cast;
    }

    private async Task<IReadOnlyList<AthleteRequest>> SquadAsync(
        string sportSlug,
        string leagueSlug,
        string teamId,
        int seasonYear,
        Dictionary<(string, string), IReadOnlyList<AthleteRequest>> squads,
        CancellationToken ct)
    {
        if (squads.TryGetValue((leagueSlug, teamId), out var cached))
            return cached;

        return squads[(leagueSlug, teamId)] = await EspnSquad.FetchAsync(
            transport, sportSlug, leagueSlug, teamId, seasonYear, logger, ct);
    }

    private async Task<List<EspnScoreboardEvent>> FetchSlateAsync(
        string sportSlug, string leagueSlug, DateOnly date, CancellationToken ct)
    {
        try
        {
            var json = await transport.GetStringAsync(
                EspnEndpoints.ScoreboardForDate(sportSlug, leagueSlug, date.ToString("yyyyMMdd")), ct);

            return JsonSerializer.Deserialize<EspnScoreboardRoot>(json)?.Events ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not CatalogSourceThrottledException)
        {
            logger.LogWarning(ex, "Failed to read the {League} scoreboard for {Date}.", leagueSlug, date);
            return [];
        }
    }
}
