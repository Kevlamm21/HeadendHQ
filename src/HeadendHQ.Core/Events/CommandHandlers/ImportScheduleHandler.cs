using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using HeadendHQ.Core.Catalog.Broadcasters.Specifications;
using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues.Specifications;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record ImportScheduleCommand : ICommand<ImportScheduleResult>;

public record ImportScheduleResult(int EventsUpserted, int EventsRemoved, List<string> Errors);

public class ImportScheduleHandler(
    IEnumerable<IScheduleSource> sources,
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<ImportScheduleHandler> logger)
    : ICommandHandler<ImportScheduleCommand, ImportScheduleResult>
{
    private record KeptEvent(
        ScheduledEventDescriptor Descriptor,
        League League,
        Broadcaster Broadcaster,
        CompetitorDescriptor Home,
        CompetitorDescriptor Away);

    public async ValueTask<ImportScheduleResult> Handle(ImportScheduleCommand command, CancellationToken ct)
    {
        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);
        var now = DateTime.UtcNow;
        var from = DateOnly.FromDateTime(now);
        var to = from.AddDays(settings.ScrapeWindowDays);

        var followedLeagues = (await workspace.Load(new FollowedLeaguesSpec(), ct)).ToList();
        var followedTeams = (await workspace.Load(new FollowedTeamsSpec(), ct)).ToList();
        var subscribed = (await workspace.Load(new SubscribedBroadcastersSpec(), ct)).ToList();

        if (followedLeagues.Count == 0)
        {
            logger.LogWarning("No leagues are followed; nothing to import.");
            return new ImportScheduleResult(0, 0, []);
        }

        var query = new ScheduleQuery(
            from, to,
            [.. followedLeagues.Select(l => l.Slug)],
            [.. subscribed.SelectMany(b => new[] { b.Slug }.Concat(b.Aliases)).Distinct()]);

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

        var upserted = 0;

        var ordered = kept.OrderBy(k => k.Descriptor.StartUtc).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var game = ordered[i];

            try
            {
                await IngestAsync(source.SourceKey, game, ct);
                upserted++;
            }
            catch (CatalogSourceThrottledException ex)
            {
                logger.LogWarning(ex, "{Source}: throttled; {Remaining} event(s) left for the next run.",
                    source.SourceKey, ordered.Count - i);
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

        var removed = await ReconcileAsync(source.SourceKey, seenExternalIds, now, ct);
        logger.LogInformation(
            "{Source}: {Kept} followed event(s) of {Listed} listed, {Upserted} upserted, {Removed} removed.",
            source.SourceKey, kept.Count, descriptors.Count, upserted, removed);

        return (upserted, removed);
    }

    private async Task<List<KeptEvent>> SelectAsync(
        IReadOnlyList<ScheduledEventDescriptor> descriptors,
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

            var broadcaster = await ResolveBroadcasterAsync(descriptor, ct);
            if (broadcaster is null || !broadcaster.IsSubscribed)
                continue;

            var home = descriptor.Competitors.FirstOrDefault(c => c.IsHome);
            var away = descriptor.Competitors.FirstOrDefault(c => !c.IsHome);
            if (home is null || away is null)
                continue;

            kept.Add(new KeptEvent(descriptor, league, broadcaster, home, away));
        }

        return kept;
    }

    private async Task IngestAsync(string sourceKey, KeptEvent game, CancellationToken ct)
    {
        var (descriptor, league, broadcaster, home, away) = game;

        var homeTeam = await ResolveTeamAsync(league, home, sourceKey, ct);
        var awayTeam = await ResolveTeamAsync(league, away, sourceKey, ct);

        var existing = await workspace.LoadSingleOrDefault(
            new SportingEventBySourceIdSpec(sourceKey, descriptor.ExternalId), ct);

        EventDetail? detail = null;
        if (existing is null || existing.NeedsDetails)
            detail = await mediator.Send(new FetchEventDetailQuery(
                league.Id, descriptor.ExternalId, descriptor.StartUtc,
                homeTeam.Id, home.DisplayName, awayTeam.Id, away.DisplayName), ct);

        var sportingEvent = existing
            ?? new SportingEvent(sourceKey, descriptor.ExternalId, league.Id, descriptor.StartUtc);

        sportingEvent.SetSchedule(descriptor.StartUtc, null);
        sportingEvent.SetParticipants(
            homeTeam.Id, home.DisplayName, awayTeam.Id, away.DisplayName, broadcaster.Id);
        sportingEvent.SetSeason(descriptor.SeasonYear, descriptor.SeasonType);
        sportingEvent.SetWatchUrl(descriptor.WatchUrl);

        if (detail is not null)
            sportingEvent.ApplyDetail(detail);
        else
            sportingEvent.RefreshVariant();

        if (existing is null)
            workspace.Add(sportingEvent);

        await unitOfWork.SaveChanges(ct);

        if (sportingEvent.NeedsTitle && LocalDay.IsToday(sportingEvent.StartUtc))
            await mediator.Send(new ProduceTitleForEventCommand(sportingEvent.Id), ct);
    }

    private async Task<Broadcaster?> ResolveBroadcasterAsync(
        ScheduledEventDescriptor descriptor, CancellationToken ct)
    {
        Broadcaster? firstKnown = null;

        foreach (var candidate in descriptor.Broadcasts.OrderBy(b => b.Priority))
        {
            var broadcaster = await mediator.Send(new ResolveBroadcasterCommand(
                candidate.ExternalId, candidate.Slug, candidate.Name, candidate.Kind), ct);

            if (broadcaster.IsSubscribed)
                return broadcaster;

            firstKnown ??= broadcaster;
        }

        return firstKnown;
    }

    private async Task<Team> ResolveTeamAsync(
        League league, CompetitorDescriptor competitor, string sourceKey, CancellationToken ct)
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
        string sourceKey, HashSet<string> seenExternalIds, DateTime now, CancellationToken ct)
    {
        if (seenExternalIds.Count == 0)
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
