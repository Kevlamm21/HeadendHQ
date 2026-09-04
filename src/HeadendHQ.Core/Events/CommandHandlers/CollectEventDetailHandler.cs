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
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record CollectEventDetailCommand(Guid SportingEventId) : ICommand<Unit>;

/// <summary>
/// The second, per-event lookup: venue, competition note, series, and the billed cast.
/// <para>
/// Players are never stored. They are read out of the source's response, ranked, and the handful
/// that get billed are written onto the event as plain names and roles — everything downstream
/// needs, and nothing to join back to. Only their faces persist, and those are content-addressed
/// and keyed by source URL, so a player is downloaded once and never again.
/// </para>
/// </summary>
public class CollectEventDetailHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IEventDetailSource detailSource,
    ISportsCatalogSource catalogSource,
    ILogger<CollectEventDetailHandler> logger)
    : ICommandHandler<CollectEventDetailCommand, Unit>
{
    public async ValueTask<Unit> Handle(CollectEventDetailCommand command, CancellationToken ct)
    {
        var sportingEvent = (await workspace.Load(
            new EntityByIdSpecification<SportingEvent, Guid>(command.SportingEventId), ct)).FirstOrDefault();

        if (sportingEvent is null)
        {
            logger.LogWarning("Sporting event {Id} no longer exists; skipping detail.", command.SportingEventId);
            return Unit.Value;
        }

        var league = await workspace.LoadById<League, int>(sportingEvent.LeagueId, ct);
        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);

        var key = new EventKey(sport.Slug, league.Slug, sportingEvent.ExternalId);
        var detail = await detailSource.GetDetailAsync(key, ct);

        var candidates = (detail?.Cast ?? []).ToList();

        // The summary only carries a cast once a game is close or under way. For anything further
        // out we fall back to team rosters, which is the only branch that costs extra requests.
        if (candidates.Count == 0)
            candidates = await FromRostersAsync(sportingEvent, league, sport, ct);

        var billed = CastRanker.Rank(candidates, sport.Slug, settings.MaxAthletesPerTeam);
        var cast = new List<BilledAthlete>();

        foreach (var candidate in billed)
            cast.Add(await BillAsync(candidate, sportingEvent, league.Id, ct));

        sportingEvent.SetCast(cast);

        // Recorded even when the source told us nothing, so a game with no venue on file is not
        // re-requested every single night.
        sportingEvent.ApplyDetails(
            detail?.VenueName, detail?.Note, detail?.SeriesType, detail?.SeriesSummary,
            LeagueVariantResolver.Resolve(detail?.Note, sportingEvent.SeasonType));

        sportingEvent.SetSeason(detail?.SeasonYear, detail?.SeasonType);

        await unitOfWork.SaveChanges(ct);

        logger.LogInformation(
            "Collected detail for {Away} at {Home} ({League}): {Cast} billed, variant {Variant}.",
            sportingEvent.AwayTeamName, sportingEvent.HomeTeamName, league.Slug,
            cast.Count, sportingEvent.Variant);

        // This runs on a queue, so it usually finishes after the nightly sweep has already looked
        // for events to produce. Without this a game scraped at 6am would not get a title until
        // tomorrow, by which point it has been played. Producing is idempotent — an event that
        // already has a title is skipped — so this and the sweep can both fire safely.
        if (IsDueToday(sportingEvent.StartUtc))
            await mediator.Send(new ProduceTitleForEventCommand(sportingEvent.Id), ct);

        return Unit.Value;
    }

    /// <summary>
    /// Flattens one ranked candidate into the row the title will be built from, downloading their
    /// face if this is the first time anyone has needed it.
    /// </summary>
    private async Task<BilledAthlete> BillAsync(
        CastCandidate candidate, SportingEvent sportingEvent, int leagueId, CancellationToken ct)
    {
        // The side is known here, so the team half of the role comes off the event rather than off
        // the source's naming — it then reads the same as the event's own team names.
        var teamName = candidate.IsHome ? sportingEvent.HomeTeamName : sportingEvent.AwayTeamName;

        var role = string.Join(", ", new[] { candidate.Athlete.Position, teamName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        // Tagged with the league so a media-day refresh can wipe one league's faces and no others.
        var headshotImageId = candidate.Athlete.HeadshotUrl is { Length: > 0 } url
            ? await mediator.Send(new MaterializeImageByUrlCommand(url, ImagePurpose.Headshot, leagueId), ct)
            : null;

        return new BilledAthlete(
            candidate.Athlete.DisplayName, role.Length > 0 ? role : null, headshotImageId);
    }

    /// <summary>
    /// Compared in local time: "due today" means the user's day, not UTC's. The stored kind is
    /// Unspecified once SQLite has round-tripped it, so it is pinned before converting.
    /// </summary>
    private static bool IsDueToday(DateTime startUtc) =>
        DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime().Date == DateTime.Now.Date;

    /// <summary>
    /// The two calls are a pair and neither stands alone: the roster says who the players are but
    /// not who starts, and the depth chart is nothing but athlete ids and ranks. Joined here, in
    /// memory, and thrown away — the ranked handful is the only part worth keeping.
    /// </summary>
    private async Task<List<CastCandidate>> FromRostersAsync(
        SportingEvent sportingEvent, League league, Sport sport, CancellationToken ct)
    {
        var candidates = new List<CastCandidate>();

        foreach (var (teamId, teamName, isHome) in new[]
                 {
                     (sportingEvent.HomeTeamId, sportingEvent.HomeTeamName, true),
                     (sportingEvent.AwayTeamId, sportingEvent.AwayTeamName, false),
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
            var depth = await catalogSource.GetDepthChartAsync(key, sportingEvent.StartUtc.Year, ct);

            foreach (var athlete in roster)
                candidates.Add(new CastCandidate(
                    athlete, isHome, teamName, externalId,
                    DepthRank: depth.TryGetValue(athlete.ExternalId, out var rank) ? rank : null));
        }

        return candidates;
    }
}
