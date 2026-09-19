using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record EventDetailTarget(
    string ExternalId,
    int LeagueId,
    DateTime StartUtc,
    int? HomeTeamId,
    string HomeTeamName,
    int? AwayTeamId,
    string AwayTeamName)
{
    public static EventDetailTarget For(SportingEvent ev) => new(
        ev.ExternalId, ev.LeagueId, ev.StartUtc,
        ev.HomeTeamId, ev.HomeTeamName, ev.AwayTeamId, ev.AwayTeamName);
}

// Games the source had nothing for are omitted rather than returned empty, so the caller leaves
// DetailsFetchedAtUtc unset and the next run retries them.
public record FetchEventDetailsQuery(IReadOnlyList<EventDetailTarget> Targets)
    : IQuery<IReadOnlyDictionary<string, EventDetail>>;

public class FetchEventDetailsHandler(
    IWorkspace workspace,
    IMediator mediator,
    IEventDetailSource detailSource)
    : IQueryHandler<FetchEventDetailsQuery, IReadOnlyDictionary<string, EventDetail>>
{
    private readonly Dictionary<int, League> _leagues = [];
    private readonly Dictionary<int, Sport> _sports = [];
    private readonly Dictionary<int, string> _teamLabels = [];

    public async ValueTask<IReadOnlyDictionary<string, EventDetail>> Handle(
        FetchEventDetailsQuery query, CancellationToken ct)
    {
        var results = new Dictionary<string, EventDetail>(StringComparer.Ordinal);

        if (query.Targets.Count == 0)
            return results;

        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);

        var keys = new List<EventKey>(query.Targets.Count);
        foreach (var target in query.Targets)
        {
            var (league, sport) = await LeagueAndSportAsync(target.LeagueId, ct);

            keys.Add(new EventKey(
                sport.Slug, league.Slug, target.ExternalId, DateOnly.FromDateTime(target.StartUtc)));
        }

        // A throttle part-way through must not discard the work already done, so keep whatever the
        // source managed to return and leave the rest for the next run.
        IReadOnlyDictionary<string, EventDetailRequest> details;
        try
        {
            details = await detailSource.GetDetailsAsync(keys, ct);
        }
        catch (CatalogSourceThrottledException)
        {
            return results;
        }

        foreach (var target in query.Targets)
        {
            if (!details.TryGetValue(target.ExternalId, out var detail))
                continue;

            var sport = _sports[_leagues[target.LeagueId].SportId];
            var billed = CastRanker.Rank(detail.Cast ?? [], sport.Slug, settings.MaxAthletesPerTeam);

            var homeLabel = await TeamLabelAsync(target.HomeTeamId, target.HomeTeamName, ct);
            var awayLabel = await TeamLabelAsync(target.AwayTeamId, target.AwayTeamName, ct);

            var cast = new List<BilledAthlete>();

            try
            {
                // Billing pulls each headshot through the image store, so it can trip the gate too.
                foreach (var candidate in billed)
                    cast.Add(await BillAsync(
                        candidate, candidate.IsHome ? homeLabel : awayLabel, target.LeagueId, ct));
            }
            catch (CatalogSourceThrottledException)
            {
                return results;
            }

            results[target.ExternalId] = new EventDetail(detail, cast);
        }

        return results;
    }

    private async Task<BilledAthlete> BillAsync(
        CastRequest candidate, string teamLabel, int leagueId, CancellationToken ct)
    {
        var role = string.Join(" - ", new[] { candidate.Athlete.Position, teamLabel }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        var headshotImageId = candidate.Athlete.HeadshotUrl is { Length: > 0 } url
            ? await mediator.Send(new MaterializeImageByUrlCommand(url, ImagePurpose.Headshot, leagueId), ct)
            : null;

        return new BilledAthlete(
            candidate.Athlete.DisplayName, role.Length > 0 ? role : null, headshotImageId);
    }

    private async Task<(League League, Sport Sport)> LeagueAndSportAsync(int leagueId, CancellationToken ct)
    {
        if (!_leagues.TryGetValue(leagueId, out var league))
            _leagues[leagueId] = league = await workspace.LoadById<League, int>(leagueId, ct);

        if (!_sports.TryGetValue(league.SportId, out var sport))
            _sports[league.SportId] = sport = await workspace.LoadById<Sport, int>(league.SportId, ct);

        return (league, sport);
    }

    private async Task<string> TeamLabelAsync(int? teamId, string storedName, CancellationToken ct)
    {
        if (teamId is not { } id)
            return storedName;

        if (_teamLabels.TryGetValue(id, out var cached))
            return cached;

        var team = await workspace.LoadById<Team, int>(id, ct);

        return _teamLabels[id] = team.Nickname is { Length: > 0 } nickname ? nickname
            : team.ShortDisplayName is { Length: > 0 } shortName ? shortName
            : team.DisplayName;
    }
}
