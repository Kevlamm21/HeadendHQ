using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record FetchEventDetailQuery(
    int LeagueId,
    string ExternalId,
    DateTime StartUtc,
    int? HomeTeamId,
    string HomeTeamName,
    int? AwayTeamId,
    string AwayTeamName) : IQuery<EventDetail>
{
    public static FetchEventDetailQuery For(SportingEvent ev) => new(
        ev.LeagueId, ev.ExternalId, ev.StartUtc,
        ev.HomeTeamId, ev.HomeTeamName, ev.AwayTeamId, ev.AwayTeamName);
}

public class FetchEventDetailHandler(
    IWorkspace workspace,
    IMediator mediator,
    IEventDetailSource detailSource,
    ISportsCatalogSource catalogSource)
    : IQueryHandler<FetchEventDetailQuery, EventDetail>
{
    public async ValueTask<EventDetail> Handle(FetchEventDetailQuery query, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(query.LeagueId, ct);
        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);

        var detail = await detailSource.GetDetailAsync(
            new EventKey(sport.Slug, league.Slug, query.ExternalId), ct);

        var candidates = (detail?.Cast ?? []).ToList();

        if (candidates.Count == 0)
            candidates = await FromRostersAsync(query, league, sport, ct);

        var billed = CastRanker.Rank(candidates, sport.Slug, settings.MaxAthletesPerTeam);
        var cast = new List<BilledAthlete>();

        var homeLabel = await TeamLabelAsync(query.HomeTeamId, query.HomeTeamName, ct);
        var awayLabel = await TeamLabelAsync(query.AwayTeamId, query.AwayTeamName, ct);

        foreach (var candidate in billed)
            cast.Add(await BillAsync(
                candidate, candidate.IsHome ? homeLabel : awayLabel, league.Id, ct));

        return new EventDetail(detail, cast);
    }

    private async Task<string> TeamLabelAsync(int? teamId, string storedName, CancellationToken ct)
    {
        if (teamId is not { } id)
            return storedName;

        var team = await workspace.LoadById<Team, int>(id, ct);

        return team.Nickname is { Length: > 0 } nickname ? nickname
            : team.ShortDisplayName is { Length: > 0 } shortName ? shortName
            : team.DisplayName;
    }

    private async Task<BilledAthlete> BillAsync(
        CastCandidate candidate, string teamLabel, int leagueId, CancellationToken ct)
    {
        var role = string.Join(" - ", new[] { candidate.Athlete.Position, teamLabel }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        var headshotImageId = candidate.Athlete.HeadshotUrl is { Length: > 0 } url
            ? await mediator.Send(new MaterializeImageByUrlCommand(url, ImagePurpose.Headshot, leagueId), ct)
            : null;

        return new BilledAthlete(
            candidate.Athlete.DisplayName, role.Length > 0 ? role : null, headshotImageId);
    }

    private async Task<List<CastCandidate>> FromRostersAsync(
        FetchEventDetailQuery query, League league, Sport sport, CancellationToken ct)
    {
        var candidates = new List<CastCandidate>();

        foreach (var (teamId, teamName, isHome) in new[]
                 {
                     (query.HomeTeamId, query.HomeTeamName, true),
                     (query.AwayTeamId, query.AwayTeamName, false),
                 })
        {
            if (teamId is not { } id)
                continue;

            var team = await workspace.LoadById<Team, int>(id, ct);
            var externalId = team.ExternalIdFor(catalogSource.SourceKey);

            if (externalId is null)
                continue;

            var key = new TeamKey(new LeagueKey(sport.Slug, league.Slug), externalId);

            var roster = await catalogSource.GetRosterAsync(key, ct);
            var depth = await catalogSource.GetDepthChartAsync(key, query.StartUtc.Year, ct);

            foreach (var athlete in roster)
                candidates.Add(new CastCandidate(
                    athlete, isHome, teamName, externalId,
                    DepthRank: depth.TryGetValue(athlete.ExternalId, out var rank) ? rank : null));
        }

        return candidates;
    }
}
