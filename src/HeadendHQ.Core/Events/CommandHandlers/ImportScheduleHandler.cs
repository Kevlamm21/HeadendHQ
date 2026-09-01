using HeadendHQ.Core.Catalog;
using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record ImportScheduleCommand : ICommand<ImportScheduleResult>;

public record ImportScheduleResult(int EventsUpserted, int EventsRemoved, List<string> Errors);

/// <summary>
/// Turns whatever the schedule sources report into <see cref="SportingEvent"/> rows.
/// <para>
/// Source-agnostic by construction: it only ever sees the neutral descriptors, so adding a second
/// source means implementing <see cref="IScheduleSource"/> and nothing else. Detail collection is
/// deliberately not done here — the scrape stays short, and each event's extra lookup is queued so a
/// single failing game cannot abandon the run.
/// </para>
/// </summary>
public class ImportScheduleHandler(
    IEnumerable<IScheduleSource> sources,
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IEventDetailQueue detailQueue,
    ILogger<ImportScheduleHandler> logger)
    : ICommandHandler<ImportScheduleCommand, ImportScheduleResult>
{
    public async ValueTask<ImportScheduleResult> Handle(ImportScheduleCommand command, CancellationToken ct)
    {
        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);
        var now = DateTime.UtcNow;
        var from = DateOnly.FromDateTime(now);
        var to = from.AddDays(settings.ScrapeWindowDays);

        var followedLeagues = (await workspace.Load(new FollowedLeaguesSpec(), ct)).ToList();
        var followedTeamIds = (await workspace.Load(new FollowedTeamsSpec(), ct)).Select(t => t.Id).ToHashSet();
        var subscribed = (await workspace.Load(new SubscribedBroadcastersSpec(), ct)).ToList();

        if (followedLeagues.Count == 0)
        {
            logger.LogWarning("No leagues are followed; nothing to import.");
            return new ImportScheduleResult(0, 0, []);
        }

        var query = new ScheduleQuery(
            from, to,
            [.. followedLeagues.Select(l => l.Slug)],
            // Every product slug a subscribed broadcaster answers to. A hint only — the source must
            // still return everything airing, so an unknown network becomes a row you can subscribe
            // to or map. The subscription filter is applied below, after resolution.
            [.. subscribed.SelectMany(b => new[] { b.Slug }.Concat(b.Aliases)).Distinct()]);

        var errors = new List<string>();
        var upserted = 0;
        var removed = 0;

        foreach (var source in sources)
        {
            try
            {
                var (added, deleted) = await ImportFromAsync(source, query, followedLeagues, followedTeamIds, now, ct);
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
        HashSet<int> followedTeamIds,
        DateTime now,
        CancellationToken ct)
    {
        var descriptors = await source.GetEventsAsync(query, ct);
        var leaguesBySlug = followedLeagues.ToDictionary(l => l.Slug, StringComparer.OrdinalIgnoreCase);

        var seenExternalIds = new HashSet<string>();
        var detailEventIds = new List<Guid>();
        var upserted = 0;

        foreach (var descriptor in descriptors)
        {
            if (!leaguesBySlug.TryGetValue(descriptor.LeagueSlug, out var league))
                continue;

            var broadcaster = await ResolveBroadcasterAsync(descriptor, ct);
            if (broadcaster is null || !broadcaster.IsSubscribed)
                continue;

            var home = descriptor.Competitors.FirstOrDefault(c => c.IsHome);
            var away = descriptor.Competitors.FirstOrDefault(c => !c.IsHome);
            if (home is null || away is null)
                continue;

            var homeTeam = await ResolveTeamAsync(league, home, source.SourceKey, ct);
            var awayTeam = await ResolveTeamAsync(league, away, source.SourceKey, ct);

            // A followed league takes everything; otherwise the game has to involve a followed team.
            if (!league.IsFollowed &&
                !followedTeamIds.Contains(homeTeam?.Id ?? -1) &&
                !followedTeamIds.Contains(awayTeam?.Id ?? -1))
                continue;

            seenExternalIds.Add(descriptor.ExternalId);

            var sportingEvent = await UpsertAsync(
                source.SourceKey, descriptor, league, homeTeam, home, awayTeam, away, broadcaster, ct);

            upserted++;

            if (sportingEvent.NeedsDetails)
                detailEventIds.Add(sportingEvent.Id);
        }

        await unitOfWork.SaveChanges(ct);

        foreach (var eventId in detailEventIds)
            detailQueue.Enqueue(eventId);

        var removed = await ReconcileAsync(source.SourceKey, seenExternalIds, now, ct);
        logger.LogInformation(
            "{Source}: {Upserted} event(s) upserted, {Removed} removed.", source.SourceKey, upserted, removed);

        return (upserted, removed);
    }

    private async Task<SportingEvent> UpsertAsync(
        string sourceKey,
        ScheduledEventDescriptor descriptor,
        League league,
        Team? homeTeam,
        CompetitorDescriptor home,
        Team? awayTeam,
        CompetitorDescriptor away,
        Broadcaster broadcaster,
        CancellationToken ct)
    {
        var existing = (await workspace.Load(
            new SportingEventBySourceIdSpec(sourceKey, descriptor.ExternalId), ct)).FirstOrDefault();

        if (existing is null)
        {
            existing = new SportingEvent(sourceKey, descriptor.ExternalId, league.Id, descriptor.StartUtc);
            workspace.Add(existing);
        }

        existing.SetSchedule(descriptor.StartUtc, null);
        existing.SetParticipants(
            homeTeam?.Id, home.DisplayName, awayTeam?.Id, away.DisplayName, broadcaster.Id);
        existing.SetSeason(descriptor.SeasonYear, descriptor.SeasonType);
        existing.SetWatchUrl(descriptor.WatchUrl);

        // Season type alone already distinguishes a playoff game; a competition note may refine it
        // later, once detail collection has run.
        existing.SetVariant(LeagueVariantResolver.Resolve(existing.Note, descriptor.SeasonType));

        return existing;
    }

    /// <summary>
    /// Finds the broadcaster behind a slug, creating it unsubscribed if it is new. Growing the list
    /// from what actually airs beats guessing it in advance, and costs one lookup per new service.
    /// </summary>
    private async Task<Broadcaster?> ResolveBroadcasterAsync(
        ScheduledEventDescriptor descriptor, CancellationToken ct)
    {
        Broadcaster? firstKnown = null;

        foreach (var candidate in descriptor.Broadcasts.OrderBy(b => b.Priority))
        {
            // Sent whether or not we already hold the row. A seeded broadcaster has no external id
            // until a schedule first names it, and without one there is no way to look up its logo —
            // which is why espn and prime-video sat there logo-less however long they existed.
            // Resolving is idempotent and costs one upstream call per broadcaster, ever.
            var broadcaster = await mediator.Send(new ResolveBroadcasterCommand(
                candidate.ExternalId, candidate.Slug, candidate.Name, candidate.Kind), ct);

            // Prefer a service we actually subscribe to over merely the highest-priority airing.
            if (broadcaster.IsSubscribed)
                return broadcaster;

            firstKnown ??= broadcaster;
        }

        return firstKnown;
    }

    private async Task<Team?> ResolveTeamAsync(
        League league, CompetitorDescriptor competitor, string sourceKey, CancellationToken ct)
    {
        if (competitor.TeamExternalId is { Length: > 0 } externalId)
        {
            var byExternalId = (await workspace.Load(new TeamByExternalIdSpec(league.Id, sourceKey, externalId), ct))
                .FirstOrDefault();

            if (byExternalId is not null)
                return byExternalId;
        }

        var byName = (await workspace.Load(new TeamByLeagueAndNameSpec(league.Id, competitor.DisplayName), ct))
            .FirstOrDefault();

        if (byName is not null)
        {
            if (competitor.TeamExternalId is { Length: > 0 } id)
                byName.TrackSource(sourceKey, id);

            return byName;
        }

        // An unknown team should never block a scrape — a mid-season expansion or a college side we
        // have not pulled yet still gets an event, with logos filled in by the next league refresh.
        var team = new Team(league.Id, competitor.DisplayName);
        if (competitor.TeamExternalId is { Length: > 0 } newId)
            team.TrackSource(sourceKey, newId);

        foreach (var candidate in competitor.Logos ?? [])
            team.UpsertLogo(candidate.Rel, sourceKey, candidate.Url, candidate.UpdatedAtUtc);

        workspace.Add(team);
        logger.LogInformation("Created previously unseen team {Team} in {League}.",
            competitor.DisplayName, league.Slug);

        return team;
    }

    /// <summary>
    /// Removes events the source has stopped listing — a cancelled game, or a playoff fixture that
    /// turned out not to be needed. Guarded against an empty response, which would otherwise read as
    /// "everything was cancelled".
    /// </summary>
    private async Task<int> ReconcileAsync(
        string sourceKey, HashSet<string> seenExternalIds, DateTime now, CancellationToken ct)
    {
        if (seenExternalIds.Count == 0)
        {
            logger.LogWarning("{Source} returned no events; skipping reconciliation.", sourceKey);
            return 0;
        }

        var future = await workspace.Load(new FutureEventsBySourceSpec(sourceKey, now), ct);
        var removed = 0;

        foreach (var sportingEvent in future.Where(e => !seenExternalIds.Contains(e.ExternalId)))
        {
            workspace.Remove(sportingEvent);
            removed++;
        }

        if (removed > 0)
            await unitOfWork.SaveChanges(ct);

        return removed;
    }
}
