using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record CollectEventDetailCommand(Guid SportingEventId) : ICommand<Unit>;

/// <summary>
/// The second, per-event lookup: venue, competition note, series, and the billed cast.
/// <para>
/// Kept to as few upstream calls as possible. The summary is one request per event and has nothing
/// to share, but rosters and depth charts are per <em>team</em> — so they are cached on the team row
/// with a TTL, and a team playing three games this week is fetched once rather than three times.
/// Headshots are content-addressed, so a player's face is downloaded once and never again.
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
        var sourceSettings = await mediator.Send(new GetSourceSettingsQuery(), ct);

        var key = new EventKey(sport.Slug, league.Slug, sportingEvent.ExternalId);
        var detail = await detailSource.GetDetailAsync(key, ct);

        var candidates = (detail?.Cast ?? []).ToList();

        // The summary only carries a cast once a game is close or under way. For anything further
        // out we fall back to team rosters, which is the only branch that costs extra requests.
        if (candidates.Count == 0)
            candidates = await FromRostersAsync(sportingEvent, league, sport, sourceSettings.RosterTtlDays, ct);

        var billed = CastRanker.Rank(candidates, sport.Slug, settings.MaxAthletesPerTeam);
        var athleteIds = new List<int>();

        foreach (var candidate in billed)
            athleteIds.Add(await UpsertAthleteAsync(
                candidate,
                league.Id,
                candidate.IsHome ? sportingEvent.HomeTeamId : sportingEvent.AwayTeamId,
                ct));

        sportingEvent.SetCast(athleteIds);

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
            athleteIds.Count, sportingEvent.Variant);

        // This runs on a queue, so it usually finishes after the nightly sweep has already looked
        // for events to produce. Without this a game scraped at 6am would not get a title until
        // tomorrow, by which point it has been played. Producing is idempotent — an event that
        // already has a title is skipped — so this and the sweep can both fire safely.
        if (IsDueToday(sportingEvent.StartUtc))
            await mediator.Send(new ProduceTitleForEventCommand(sportingEvent.Id), ct);

        return Unit.Value;
    }

    /// <summary>
    /// Compared in local time: "due today" means the user's day, not UTC's. The stored kind is
    /// Unspecified once SQLite has round-tripped it, so it is pinned before converting.
    /// </summary>
    private static bool IsDueToday(DateTime startUtc) =>
        DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime().Date == DateTime.Now.Date;

    private async Task<List<CastCandidate>> FromRostersAsync(
        SportingEvent sportingEvent, League league, Sport sport, int rosterTtlDays, CancellationToken ct)
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
            var externalId = team.ExternalRefs.ExternalIdFor(catalogSource.SourceKey);

            if (externalId is null)
                continue;

            var key = new TeamKey(new LeagueKey(sport.Slug, league.Slug), externalId);

            // The TTL is what keeps this cheap: several games for the same team in one window share
            // a single roster fetch, and a nightly re-run does not refetch a roster from yesterday.
            if (team.RosterIsStale(rosterTtlDays))
            {
                var roster = await catalogSource.GetRosterAsync(key, ct);
                var depth = await catalogSource.GetDepthChartAsync(key, sportingEvent.StartUtc.Year, ct);

                foreach (var athlete in roster)
                    candidates.Add(new CastCandidate(
                        athlete, isHome, teamName, externalId,
                        DepthRank: depth.TryGetValue(athlete.ExternalId, out var rank) ? rank : null));

                team.MarkRosterRefreshed();
                await StoreRosterAsync(roster, league.Id, team.Id, ct);
            }
            else
            {
                // Already stored; rebuild candidates from what we hold rather than asking again.
                foreach (var athlete in await LoadStoredAthletesAsync(team.Id, ct))
                    candidates.Add(new CastCandidate(athlete, isHome, teamName, externalId));
            }
        }

        return candidates;
    }

    private async Task StoreRosterAsync(
        IReadOnlyList<AthleteDescriptor> roster, int leagueId, int teamId, CancellationToken ct)
    {
        foreach (var descriptor in roster)
            await UpsertAthleteRowAsync(descriptor, leagueId, teamId, ct);

        await unitOfWork.SaveChanges(ct);
    }

    private async Task<List<AthleteDescriptor>> LoadStoredAthletesAsync(int teamId, CancellationToken ct)
    {
        var athletes = await workspace.Load(new AthletesByTeamSpec(teamId), ct);

        return [.. athletes.Select(a => new AthleteDescriptor(
            a.ExternalRefs.ExternalIdFor(catalogSource.SourceKey) ?? a.Id.ToString(),
            a.DisplayName, a.ShortName, a.Position, a.Jersey, a.ExperienceYears,
            a.Headshot.SourceUrl))];
    }

    private async Task<int> UpsertAthleteAsync(
        CastCandidate candidate, int leagueId, int? teamId, CancellationToken ct)
    {
        // The side is known here, so pass it through: sending null would blank the team the roster
        // pass just recorded, and the cast's roles are built by matching an athlete's team against
        // the event's two.
        var athlete = await UpsertAthleteRowAsync(candidate.Athlete, leagueId, teamId, ct);

        // Headshots are content-addressed: this downloads once per player, ever, and a later refresh
        // that hashes the same costs a 304 and no write.
        if (!athlete.Headshot.IsMaterialized && athlete.Headshot.SourceUrl is not null)
            await mediator.Send(new MaterializeImageCommand(athlete.Headshot, ImagePurpose.Headshot), ct);

        return athlete.Id;
    }

    private async Task<Athlete> UpsertAthleteRowAsync(
        AthleteDescriptor descriptor, int leagueId, int? teamId, CancellationToken ct)
    {
        var athlete = (await workspace.Load(
            new AthleteByExternalIdSpec(catalogSource.SourceKey, descriptor.ExternalId), ct)).FirstOrDefault();

        if (athlete is null)
        {
            athlete = new Athlete(leagueId, descriptor.DisplayName);
            athlete.TrackSource(catalogSource.SourceKey, descriptor.ExternalId);
            workspace.Add(athlete);
            await unitOfWork.SaveChanges(ct);
        }

        // The latest sighting wins, which is how a trade heals itself without a nightly roster job.
        athlete.Describe(
            descriptor.DisplayName, descriptor.ShortName, descriptor.Position,
            descriptor.Jersey, descriptor.ExperienceYears, teamId ?? athlete.TeamId);

        if (descriptor.HeadshotUrl is { Length: > 0 } url && !athlete.Headshot.IsMaterialized)
            athlete.PointHeadshotAt(catalogSource.SourceKey, url);

        return athlete;
    }
}
