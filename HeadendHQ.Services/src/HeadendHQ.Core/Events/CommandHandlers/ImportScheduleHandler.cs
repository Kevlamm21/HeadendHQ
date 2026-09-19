using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Leagues.Specifications;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Streaming;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record ImportScheduleCommand : ICommand<ImportScheduleResult>;

public record ImportScheduleResult(int EventsUpserted, int EventsRemoved, List<string> Errors);

public class ImportScheduleHandler(
    IEnumerable<IScheduleSource> sources,
    IWorkspace workspace,
    IReadModel readModel,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<ImportScheduleHandler> logger)
    : ICommandHandler<ImportScheduleCommand, ImportScheduleResult>
{
    private record KeptEvent(
        SportingEventRequest Descriptor,
        League League,
        IReadOnlyList<(Broadcaster Broadcaster, int Priority)> Broadcasts,
        CompetitorRequest Home,
        CompetitorRequest Away);

    private record PreparedEvent(
        KeptEvent Game,
        Team HomeTeam,
        Team AwayTeam,
        StreamingChoice Choice,
        SportingEvent? Existing)
    {
        public SportingEventRequest Descriptor => Game.Descriptor;

        public bool NeedsDetail => Existing is null || Existing.NeedsDetails;
    }

    public async ValueTask<ImportScheduleResult> Handle(ImportScheduleCommand command, CancellationToken ct)
    {
        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);
        var now = DateTime.UtcNow;
        var from = DateOnly.FromDateTime(now);
        var to = from.AddDays(settings.ScrapeWindowDays);

        var followedLeagues = (await workspace.Load(new FollowedLeaguesSpec(), ct)).ToList();
        var followedTeams = (await workspace.Load(new FollowedTeamsSpec(), ct)).ToList();

        if (followedLeagues.Count == 0)
        {
            logger.LogWarning("No leagues are followed; nothing to import.");
            return new ImportScheduleResult(0, 0, []);
        }

        var query = new ScheduleQuery(from, to, [.. followedLeagues.Select(l => l.Slug)]);

        var errors = new List<string>();
        var upserted = 0;
        var removed = 0;

        foreach (var source in sources)
        {
            try
            {
                var filter = new EventFollowFilter(followedTeams, source.SourceKey);
                var (added, deleted) = await ImportFromAsync(source, query, followedLeagues, filter, now, errors, ct);
                upserted += added;
                removed += deleted;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Schedule import failed for {Source}.", source.SourceKey);
                errors.Add($"{source.SourceKey}: {ex.Message}");
            }
        }

        return new ImportScheduleResult(upserted, removed, errors);
    }

    private async Task<(int Upserted, int Removed)> ImportFromAsync(
        IScheduleSource source,
        ScheduleQuery query,
        List<League> followedLeagues,
        EventFollowFilter filter,
        DateTime now,
        List<string> errors,
        CancellationToken ct)
    {
        var descriptors = await source.GetEventsAsync(query, ct);
        var kept = await SelectAsync(descriptors, followedLeagues, filter, ct);

        var seenExternalIds = kept.Select(k => k.Descriptor.ExternalId).ToHashSet();

        var ordered = kept.OrderBy(k => k.Descriptor.StartUtc).ToList();

        // Settle teams and streaming first so the detail batch below only covers games we will keep,
        // then fetch every game's detail and every team's squad in one pass. Nothing in the persist
        // loop afterwards touches the network.
        var (prepared, skipped) = await PrepareAsync(source.SourceKey, ordered, errors, ct);

        foreach (var externalId in skipped)
            seenExternalIds.Remove(externalId);

        var details = await mediator.Send(
            new FetchEventDetailsQuery([.. prepared.Where(p => p.NeedsDetail).Select(ToTarget)]), ct);

        var upserted = 0;

        for (var i = 0; i < prepared.Count; i++)
        {
            var game = prepared[i];

            try
            {
                details.TryGetValue(game.Descriptor.ExternalId, out var detail);
                await IngestAsync(source.SourceKey, game, detail, ct);
                upserted++;
            }
            catch (CatalogSourceThrottledException ex)
            {
                logger.LogWarning(ex, "{Source}: throttled; {Remaining} event(s) left for the next run.",
                    source.SourceKey, prepared.Count - i);
                errors.Add($"{source.SourceKey}: {ex.Message}");
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to import {Name} ({Id}) from {Source}.",
                    game.Descriptor.Name, game.Descriptor.ExternalId, source.SourceKey);
                errors.Add($"{source.SourceKey} {game.Descriptor.ExternalId}: {ex.Message}");
            }
        }

        var removed = await ReconcileAsync(source.SourceKey, kept.Count > 0, seenExternalIds, now, ct);
        logger.LogInformation(
            "{Source}: {Kept} followed event(s) of {Listed} listed, {Upserted} upserted, {Removed} removed.",
            source.SourceKey, kept.Count, descriptors.Count, upserted, removed);

        return (upserted, removed);
    }

    private static EventDetailTarget ToTarget(PreparedEvent prepared) => new(
        prepared.Descriptor.ExternalId,
        prepared.Game.League.Id,
        prepared.Descriptor.StartUtc,
        prepared.HomeTeam.Id,
        prepared.Game.Home.DisplayName,
        prepared.AwayTeam.Id,
        prepared.Game.Away.DisplayName);

    private async Task<(List<PreparedEvent> Prepared, List<string> Skipped)> PrepareAsync(
        string sourceKey, List<KeptEvent> ordered, List<string> errors, CancellationToken ct)
    {
        var prepared = new List<PreparedEvent>(ordered.Count);
        var skipped = new List<string>();

        foreach (var game in ordered)
        {
            var (descriptor, _, broadcasts, home, away) = game;

            try
            {
                var choice = await StreamingResolver.ResolveAsync(
                    readModel, descriptor.StartUtc, home.DisplayName, away.DisplayName, broadcasts, ct);

                if (choice.Outcome is not StreamingOutcome.Stream)
                {
                    logger.LogInformation("Skipping {Away} at {Home} ({Id}): {Outcome} via {Broadcaster}.",
                        away.DisplayName, home.DisplayName, descriptor.ExternalId,
                        choice.Outcome, choice.Broadcaster?.Slug);
                    skipped.Add(descriptor.ExternalId);
                    continue;
                }

                var homeTeam = await ResolveTeamAsync(game.League, home, sourceKey, ct);
                var awayTeam = await ResolveTeamAsync(game.League, away, sourceKey, ct);

                var existing = await workspace.LoadSingleOrDefault(
                    new SportingEventBySourceIdSpec(sourceKey, descriptor.ExternalId), ct);

                prepared.Add(new PreparedEvent(game, homeTeam, awayTeam, choice, existing));
            }
            catch (CatalogSourceThrottledException ex)
            {
                logger.LogWarning(ex, "{Source}: throttled while preparing; the rest waits for the next run.",
                    sourceKey);
                errors.Add($"{sourceKey}: {ex.Message}");
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to prepare {Name} ({Id}).", descriptor.Name, descriptor.ExternalId);
                errors.Add($"{sourceKey} {descriptor.ExternalId}: {ex.Message}");
                skipped.Add(descriptor.ExternalId);
            }
        }

        return (prepared, skipped);
    }

    private async Task<List<KeptEvent>> SelectAsync(
        IReadOnlyList<SportingEventRequest> descriptors,
        List<League> followedLeagues,
        EventFollowFilter filter,
        CancellationToken ct)
    {
        var leaguesBySlug = followedLeagues.ToDictionary(l => l.Slug, StringComparer.OrdinalIgnoreCase);
        var kept = new List<KeptEvent>();

        foreach (var descriptor in descriptors)
        {
            if (!leaguesBySlug.TryGetValue(descriptor.LeagueSlug, out var league))
                continue;

            if (!filter.IsFollowed(league, descriptor))
                continue;

            var home = descriptor.Competitors.FirstOrDefault(c => c.IsHome);
            var away = descriptor.Competitors.FirstOrDefault(c => !c.IsHome);
            if (home is null || away is null)
                continue;

            var broadcasts = await ResolveSubscribedBroadcastsAsync(descriptor, ct);
            if (broadcasts.Count == 0)
                continue;

            kept.Add(new KeptEvent(descriptor, league, broadcasts, home, away));
        }

        return kept;
    }

    private async Task IngestAsync(
        string sourceKey, PreparedEvent prepared, EventDetail? detail, CancellationToken ct)
    {
        var (game, homeTeam, awayTeam, choice, existing) = prepared;
        var (descriptor, league, _, home, away) = game;

        var sportingEvent = existing
            ?? new SportingEvent(sourceKey, descriptor.ExternalId, league.Id, descriptor.StartUtc);

        var startChanged = existing is not null && existing.StartUtc != descriptor.StartUtc;

        sportingEvent.SetSchedule(descriptor.StartUtc, null);
        sportingEvent.SetParticipants(homeTeam.Id, home.DisplayName, awayTeam.Id, away.DisplayName);
        sportingEvent.AssignStreaming(choice.Broadcaster!.Id, choice.Service!.Id);
        sportingEvent.SetSeason(descriptor.SeasonYear, descriptor.SeasonType);
        sportingEvent.SetWatchUrl(descriptor.WatchUrl);

        if (detail is not null)
            sportingEvent.ApplyDetail(detail);
        else
            sportingEvent.RefreshVariant();

        if (existing is null)
            workspace.Add(sportingEvent);

        await unitOfWork.SaveChanges(ct);

        if (startChanged && sportingEvent.TitleId is { } titleId)
            await mediator.Send(new UpdateTitleCommand(titleId, new UpdateTitleRequest
            {
                StartUtc = sportingEvent.StartUtc,
                EndUtc = sportingEvent.EndUtc,
            }), ct);

        if (sportingEvent.NeedsTitle && LocalDay.IsToday(sportingEvent.StartUtc))
            await mediator.Send(new ProduceTitleForEventCommand(sportingEvent.Id), ct);
    }

    private async Task<List<(Broadcaster Broadcaster, int Priority)>> ResolveSubscribedBroadcastsAsync(
        SportingEventRequest descriptor, CancellationToken ct)
    {
        var subscribed = new List<(Broadcaster Broadcaster, int Priority)>();

        foreach (var candidate in descriptor.Broadcasts)
        {
            var broadcaster = await mediator.Send(new ResolveBroadcasterCommand(
                candidate.ExternalId, candidate.Slug, candidate.Name, candidate.Kind), ct);

            if (broadcaster.IsSubscribed)
                subscribed.Add((broadcaster, candidate.Priority));
        }

        return subscribed;
    }

    private readonly HashSet<int> _logoAttempts = [];

    private async Task<Team> ResolveTeamAsync(
        League league, CompetitorRequest competitor, string sourceKey, CancellationToken ct)
    {
        var team = await FindOrCreateTeamAsync(league, competitor, sourceKey, ct);

        if (!team.HasFetchedLogos && _logoAttempts.Add(team.Id))
            await mediator.Send(new RefreshTeamLogosCommand(team.Id), ct);

        return team;
    }

    private async Task<Team> FindOrCreateTeamAsync(
        League league, CompetitorRequest competitor, string sourceKey, CancellationToken ct)
    {
        if (competitor.TeamExternalId is { Length: > 0 } externalId)
        {
            var byExternalId = (await workspace.Load(
                new TeamByExternalIdSpec(league.Id, sourceKey, externalId), ct)).FirstOrDefault();

            if (byExternalId is not null)
                return byExternalId;
        }

        var byName = (await workspace.Load(
            new TeamByLeagueAndNameSpec(league.Id, competitor.DisplayName), ct)).FirstOrDefault();

        if (byName is not null)
        {
            if (competitor.TeamExternalId is { Length: > 0 } id)
                byName.TrackSource(sourceKey, id);

            return byName;
        }

        var team = new Team(league.Id, competitor.DisplayName, isFollowed: league.IsFollowed);
        if (competitor.TeamExternalId is { Length: > 0 } newId)
            team.TrackSource(sourceKey, newId);

        workspace.Add(team);

        // The event references the team by id, and an int key isn't assigned until insert.
        await unitOfWork.SaveChanges(ct);

        logger.LogInformation("Created previously unseen team {Team} in {League}.",
            competitor.DisplayName, league.Slug);

        return team;
    }

    private async Task<int> ReconcileAsync(
        string sourceKey, bool anyKept, HashSet<string> seenExternalIds, DateTime now, CancellationToken ct)
    {
        if (!anyKept)
        {
            logger.LogWarning("{Source} returned no followed events; skipping reconciliation.", sourceKey);
            return 0;
        }

        var future = await workspace.Load(new FutureEventsBySourceSpec(sourceKey, now), ct);
        var stale = future.Where(e => !seenExternalIds.Contains(e.ExternalId)).ToArray();

        if (stale.Length > 0)
        {
            var titleIds = stale.Where(e => e.TitleId is not null).Select(e => e.TitleId!.Value).ToHashSet();
            var titles = titleIds.Count == 0
                ? []
                : (await workspace.LoadAll<Title>(ct)).Where(t => titleIds.Contains(t.Id)).ToArray();

            await TitleEventCleanup.RemoveAsync(workspace, stale, titles, ct);
            await unitOfWork.SaveChanges(ct);
        }

        return stale.Length;
    }
}
