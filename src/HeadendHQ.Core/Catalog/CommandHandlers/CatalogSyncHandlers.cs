using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

/// <summary>
/// Walks the source's sport and league catalog and mirrors it into the database.
/// <para>
/// Only names and logo <em>URLs</em> are recorded — no image is downloaded. ESPN publishes 356
/// leagues, and eagerly fetching artwork for all of them would mean thousands of requests for
/// leagues nobody follows. Bytes arrive when a league is actually followed.
/// </para>
/// <para>
/// Checkpointed per sport so an interrupted run resumes instead of starting over, and idempotent so
/// resuming cannot duplicate anything.
/// </para>
/// </summary>
public record SyncSportsAndLeaguesCommand : ICommand<CatalogSyncState>;

public class SyncSportsAndLeaguesHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<SyncSportsAndLeaguesHandler> logger)
    : ICommandHandler<SyncSportsAndLeaguesCommand, CatalogSyncState>
{
    public async ValueTask<CatalogSyncState> Handle(SyncSportsAndLeaguesCommand command, CancellationToken ct)
    {
        var state = await workspace.LoadSingleOrDefault(new CatalogSyncStateSpec(), ct)
            ?? throw new InvalidOperationException("CatalogSyncState not found.");

        var sourceSettings = await mediator.Send(new GetSourceSettingsQuery(), ct);

        state.Begin();
        state.EnterStage("Sports");
        await unitOfWork.SaveChanges(ct);

        try
        {
            var sports = await source.GetSportsAsync(ct);
            logger.LogInformation("Catalog sync: {Count} sport(s) from {Source}.", sports.Count, source.SourceKey);

            foreach (var descriptor in sports)
            {
                // Every sport is recorded whether or not we walk it, so the settings UI can offer
                // "pull lacrosse too" without a second discovery of the sport list itself.
                var sport = await UpsertSportAsync(descriptor, ct);

                if (!sourceSettings.CoversSport(descriptor.Slug))
                    continue;

                if (state.IsLeagueCompleted(descriptor.Slug))
                    continue;

                state.EnterStage($"Leagues:{descriptor.Slug}");
                await SyncLeaguesAsync(sport, descriptor.Slug, ct);

                // Checkpointed and flushed per sport, so a crash costs one sport, not the whole run.
                state.MarkLeagueCompleted(descriptor.Slug);
                await unitOfWork.SaveChanges(ct);
            }

            state.Complete();
            logger.LogInformation("Catalog sync complete.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The completion marker is deliberately not set, so the next start resumes.
            state.Fail(ex.Message);
            logger.LogError(ex, "Catalog sync failed; it will resume on the next run.");
        }

        return state;
    }

    private async Task<Sport> UpsertSportAsync(SportDescriptor descriptor, CancellationToken ct)
    {
        var sport = await workspace.LoadSingleOrDefault(new SportBySlugSpec(descriptor.Slug), ct);

        if (sport is null)
        {
            sport = new Sport(descriptor.Slug, descriptor.Name);
            workspace.Add(sport);
        }
        else
        {
            sport.Rename(descriptor.Name);
        }

        sport.TrackSource(source.SourceKey, descriptor.ExternalId);

        // The league loop below needs the sport's identity, which only exists after an insert.
        await unitOfWork.SaveChanges(ct);
        return sport;
    }

    private async Task SyncLeaguesAsync(Sport sport, string sportSlug, CancellationToken ct)
    {
        var leagues = await source.GetLeaguesAsync(sportSlug, ct);

        foreach (var descriptor in leagues)
        {
            var league = await workspace.LoadSingleOrDefault(new LeagueBySlugSpec(descriptor.Slug), ct);

            if (league is null)
            {
                league = new League(sport.Id, descriptor.Slug, descriptor.Name);
                workspace.Add(league);
            }

            league.Describe(descriptor.Name, descriptor.Abbreviation, descriptor.ShortName, descriptor.SupportsTeams);
            league.TrackSource(source.SourceKey, descriptor.ExternalId);

            foreach (var candidate in descriptor.Logos ?? [])
                league.UpsertLogo(LogoVariants.Default, candidate.Rel, source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);

        }

        logger.LogInformation("Catalog sync: {Count} league(s) for {Sport}.", leagues.Count, sportSlug);
    }
}

/// <summary>
/// Walks one sport's leagues on demand — the escape hatch for anything outside
/// <see cref="SourceSettings.DiscoverySportSlugs"/>. Idempotent, so re-running it is harmless.
/// </summary>
public record SyncSportLeaguesCommand(string SportSlug) : ICommand<int>;

public class SyncSportLeaguesHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    ISportsCatalogSource source,
    ILogger<SyncSportLeaguesHandler> logger)
    : ICommandHandler<SyncSportLeaguesCommand, int>
{
    public async ValueTask<int> Handle(SyncSportLeaguesCommand command, CancellationToken ct)
    {
        var slug = command.SportSlug.Trim().ToLowerInvariant();

        var sport = await workspace.LoadSingleOrDefault(new SportBySlugSpec(slug), ct);

        if (sport is null)
        {
            // The sport list is one request, so a sport we have never recorded is cheap to add.
            var descriptor = (await source.GetSportsAsync(ct))
                .FirstOrDefault(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotFoundException<Sport>(slug);

            sport = new Sport(descriptor.Slug, descriptor.Name);
            sport.TrackSource(source.SourceKey, descriptor.ExternalId);
            workspace.Add(sport);
            await unitOfWork.SaveChanges(ct);
        }

        var leagues = await source.GetLeaguesAsync(sport.Slug, ct);

        foreach (var descriptor in leagues)
        {
            var league = await workspace.LoadSingleOrDefault(new LeagueBySlugSpec(descriptor.Slug), ct);

            if (league is null)
            {
                league = new League(sport.Id, descriptor.Slug, descriptor.Name);
                workspace.Add(league);
            }

            league.Describe(descriptor.Name, descriptor.Abbreviation, descriptor.ShortName, descriptor.SupportsTeams);
            league.TrackSource(source.SourceKey, descriptor.ExternalId);

            foreach (var candidate in descriptor.Logos ?? [])
                league.UpsertLogo(LogoVariants.Default, candidate.Rel, source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);
        }

        await unitOfWork.SaveChanges(ct);
        logger.LogInformation("Pulled {Count} league(s) for {Sport} on request.", leagues.Count, sport.Slug);

        return leagues.Count;
    }
}

/// <summary>
/// Pulls a league's teams — one request returns every team with colours and all logo variants.
/// Run when a league is followed, and again on the nightly refresh for followed leagues.
/// </summary>
public record RefreshLeagueTeamsCommand(int LeagueId, bool MaterializeLogos = false) : ICommand<int>;

public class RefreshLeagueTeamsHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISportsCatalogSource source,
    ILogger<RefreshLeagueTeamsHandler> logger)
    : ICommandHandler<RefreshLeagueTeamsCommand, int>
{
    public async ValueTask<int> Handle(RefreshLeagueTeamsCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (!league.SupportsTeams)
        {
            logger.LogInformation("League {League} has no teams to refresh.", league.Slug);
            return 0;
        }

        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var key = new LeagueKey(sport.Slug, league.Slug, league.ExternalRefs.ExternalIdFor(source.SourceKey));

        var descriptors = await source.GetTeamsAsync(key, ct);
        var existing = (await workspace.Load(new TeamsByLeagueSpec(league.Id), ct))
            .ToDictionary(t => t.ExternalRefs.ExternalIdFor(source.SourceKey) ?? $"name:{t.DisplayName}");

        foreach (var descriptor in descriptors)
        {
            if (!existing.TryGetValue(descriptor.ExternalId, out var team))
            {
                team = new Team(league.Id, descriptor.DisplayName);
                workspace.Add(team);
                existing[descriptor.ExternalId] = team;
            }

            team.Describe(
                descriptor.DisplayName, descriptor.ShortDisplayName, descriptor.Slug, descriptor.Abbreviation,
                descriptor.Location, descriptor.Nickname, descriptor.PrimaryColorHex, descriptor.AlternateColorHex,
                descriptor.IsActive);
            team.TrackSource(source.SourceKey, descriptor.ExternalId);

            foreach (var candidate in descriptor.Logos ?? [])
                team.UpsertLogo(candidate.Rel, source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);
        }

        league.MarkTeamsRefreshed();
        await unitOfWork.SaveChanges(ct);

        await VerifyLogosAsync(key, existing.Values, ct);

        if (command.MaterializeLogos)
            foreach (var team in existing.Values)
                if (team.PreferredLogo() is { } logo)
                    await mediator.Send(new MaterializeImageCommand(logo.Image, ImagePurpose.TeamLogo), ct);

        logger.LogInformation("Refreshed {Count} team(s) for {League}.", descriptors.Count, league.Slug);
        return descriptors.Count;
    }

    /// <summary>
    /// Re-sources the logo variants the bulk listing cannot be trusted for, one team at a time.
    /// <para>
    /// ESPN's bulk NFL listing hands every team the previous team id's image guid, so all the
    /// on-colour marks come back as the wrong club. Correcting it costs one request per team, but
    /// image addresses are stable, so it happens once per team and then never again. The per-run cap
    /// is what keeps a 759-team league from turning a nightly refresh into a stampede: the remainder
    /// is simply picked up by the next run.
    /// </para>
    /// </summary>
    private async Task VerifyLogosAsync(LeagueKey key, IEnumerable<Team> teams, CancellationToken ct)
    {
        var settings = await mediator.Send(new GetSourceSettingsQuery(), ct);
        var budget = settings.MaxTeamLogoLookupsPerRun;
        var verified = 0;

        foreach (var team in teams)
        {
            if (verified >= budget)
                break;

            if (!team.LogosNeedVerifying)
                continue;

            if (team.ExternalRefs.ExternalIdFor(source.SourceKey) is not { } externalId)
                continue;

            IReadOnlyList<ImageCandidate> candidates;
            try
            {
                candidates = await source.GetTeamLogosAsync(new TeamKey(key, externalId), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreadable team must not abandon the rest; it stays unverified and is retried.
                logger.LogWarning(ex, "Failed to verify logos for team {Team}.", team.DisplayName);
                continue;
            }

            if (candidates.Count == 0)
                continue;

            // PointAt clears the stored validators when the address changes, so a wrong logo already
            // in the database is re-downloaded rather than revalidated into place.
            foreach (var candidate in candidates)
                team.UpsertLogo(candidate.Rel, source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);

            team.MarkLogosVerified();
            verified++;
        }

        if (verified > 0)
        {
            await unitOfWork.SaveChanges(ct);
            logger.LogInformation(
                "Verified logo variants for {Count} team(s) in {League}{More}.",
                verified, key.LeagueSlug,
                verified >= budget ? "; more remain for the next run" : string.Empty);
        }
    }
}

/// <summary>
/// Records a broadcaster the schedule mentioned, resolving its full record and logos the first time.
/// Unknown broadcasters are created unsubscribed, so the settings list grows to what ESPN actually
/// airs rather than what we guessed in advance.
/// </summary>
public record ResolveBroadcasterCommand(
    string ExternalId, string Slug, string Name, BroadcasterKind Kind) : ICommand<Broadcaster>;

public class ResolveBroadcasterHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IBroadcasterCatalogSource source)
    : ICommandHandler<ResolveBroadcasterCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(ResolveBroadcasterCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadSingleOrDefault(new BroadcasterBySlugSpec(command.Slug), ct);

        LineupIndex? lineup = null;

        if (broadcaster is null)
        {
            broadcaster = new Broadcaster(command.Slug, command.Name);
            broadcaster.Describe(command.Name, null, null, command.Kind);
            workspace.Add(broadcaster);

            // Classify off the slug/name now; the call letters from the detail lookup below are a
            // stronger signal, so this runs again once they are known.
            lineup = LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));
            BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
        }
        else
        {
            broadcaster.Describe(broadcaster.Name, broadcaster.ShortName, broadcaster.CallLetters, command.Kind);
        }

        // The external id only becomes known when a schedule first names the broadcaster, which is
        // why the seeded rows — espn, prime-video, peacock — sit there with no logo however long
        // they have existed: nothing had ever told them which upstream record was theirs.
        //
        // A sighting can arrive under the broadcaster's own slug or under one of its aliases, since
        // a brand's products share a row (espnplus under espn). An alias is enough to seed the
        // record — some mark beats none — but the broadcaster's own slug outranks it and replaces
        // what the alias supplied. Either way it settles after one lookup and stops churning.
        var isCanonical = broadcaster.Slug.Equals(command.Slug, StringComparison.OrdinalIgnoreCase);
        var knownExternalId = broadcaster.ExternalRefs.ExternalIdFor(source.SourceKey);

        if (!isCanonical && knownExternalId is not null)
            return broadcaster;

        if (isCanonical && knownExternalId == command.ExternalId && !broadcaster.NeedsDetail)
            return broadcaster;

        broadcaster.TrackSource(source.SourceKey, command.ExternalId);
        await unitOfWork.SaveChanges(ct);

        var detail = await source.GetBroadcasterAsync(command.ExternalId, ct);
        if (detail is not null)
        {
            // A row stands for the brand, not whichever of its products happened to air first, so an
            // alias sighting contributes artwork but must not rename ESPN to "ESPN Unlimited".
            if (isCanonical)
                broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, command.Kind);

            foreach (var candidate in detail.Logos ?? [])
                broadcaster.UpsertLogo(
                    candidate.Rel == LogoRels.Dark ? LogoRels.Dark : LogoVariants.Default,
                    source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);

            if (isCanonical)
            {
                lineup ??= LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));
                BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
            }
        }

        // Recorded even when the source had nothing, so a local affiliate with no artwork on file —
        // most of them — is not looked up again on every scrape.
        broadcaster.MarkDetailFetched();

        return broadcaster;
    }
}

/// <summary>
/// Crawls ESPN's media index for the full broadcaster catalogue — every network, with both logo
/// variants — so the settings list is complete before a game has aired on each one.
/// <para>
/// The index is two requests; each record is one more, ~1311 in total, which fits one run's request
/// budget. An interrupted crawl resumes by skipping ids already on a row, and the completion marker
/// (<see cref="CatalogSyncState.LastBroadcasterRefreshUtc"/>) is only set once the index end is
/// reached, so a truncated crawl is retried rather than stranding every network past the cut-off.
/// </para>
/// </summary>
public record DiscoverBroadcastersCommand(int? Max = null) : ICommand<DiscoverBroadcastersResult>;

public record DiscoverBroadcastersResult(int Examined, int Created, int LogosAdded, bool SweepComplete);

public class DiscoverBroadcastersHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IBroadcasterCatalogSource source,
    ILogger<DiscoverBroadcastersHandler> logger)
    : ICommandHandler<DiscoverBroadcastersCommand, DiscoverBroadcastersResult>
{
    private const int CheckpointEvery = 50;

    public async ValueTask<DiscoverBroadcastersResult> Handle(DiscoverBroadcastersCommand command, CancellationToken ct)
    {
        var state = await workspace.LoadSingleOrDefault(new CatalogSyncStateSpec(), ct)
            ?? throw new InvalidOperationException("CatalogSyncState not found.");

        var lineup = LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));

        var all = await workspace.LoadAll<Broadcaster>(ct);
        var bySlugOrAlias = new Dictionary<string, Broadcaster>(StringComparer.OrdinalIgnoreCase);
        var resolvedIds = new HashSet<string>();

        foreach (var b in all)
        {
            bySlugOrAlias.TryAdd(b.Slug, b);
            foreach (var alias in b.Aliases)
                bySlugOrAlias.TryAdd(alias, b);

            if (b.ExternalRefs.ExternalIdFor(source.SourceKey) is { } id)
                resolvedIds.Add(id);
        }

        var ids = await source.ListBroadcasterIdsAsync(ct);

        var examined = 0;
        var created = 0;
        var logosAdded = 0;
        var sinceCheckpoint = 0;
        var cappedOut = false;

        try
        {
            foreach (var id in ids)
            {
                if (resolvedIds.Contains(id))
                    continue;

                if (command.Max is { } max && examined >= max)
                {
                    cappedOut = true;
                    break;
                }

                var detail = await source.GetBroadcasterAsync(id, ct);
                examined++;

                if (detail is null || string.IsNullOrEmpty(detail.Slug))
                    continue;

                var canonical = bySlugOrAlias.TryGetValue(detail.Slug, out var existing)
                    && existing!.Slug.Equals(detail.Slug, StringComparison.OrdinalIgnoreCase);

                if (existing is not null && !canonical)
                {
                    // Alias sighting: artwork only. It must not rename the row or claim its id, or
                    // the canonical record would never be read.
                    foreach (var candidate in detail.Logos ?? [])
                        existing.UpsertLogo(LogoVariant(candidate.Rel), source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);
                    logosAdded++;
                }
                else
                {
                    var broadcaster = existing ?? new Broadcaster(detail.Slug, detail.Name);

                    broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, BroadcasterKind.Unknown);
                    broadcaster.TrackSource(source.SourceKey, detail.ExternalId);

                    foreach (var candidate in detail.Logos ?? [])
                        broadcaster.UpsertLogo(LogoVariant(candidate.Rel), source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);

                    broadcaster.MarkDetailFetched();

                    if (existing is null)
                    {
                        BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
                        workspace.Add(broadcaster);
                        bySlugOrAlias[detail.Slug] = broadcaster;
                        created++;
                    }

                    resolvedIds.Add(detail.ExternalId);
                }

                if (++sinceCheckpoint >= CheckpointEvery)
                {
                    await unitOfWork.SaveChanges(ct);
                    sinceCheckpoint = 0;
                }
            }

        }
        catch (CatalogSourceThrottledException ex)
        {
            await unitOfWork.SaveChanges(ct);
            logger.LogInformation(ex,
                "Broadcaster crawl stopped after {Examined} record(s); it resumes on the next run.", examined);
            return new DiscoverBroadcastersResult(examined, created, logosAdded, SweepComplete: false);
        }

        if (!cappedOut)
            state.MarkBroadcastersRefreshed();

        await unitOfWork.SaveChanges(ct);

        logger.LogInformation(
            "Broadcaster crawl: examined {Examined}, created {Created}, +{Logos} logo(s), complete={Complete}.",
            examined, created, logosAdded, !cappedOut);

        return new DiscoverBroadcastersResult(examined, created, logosAdded, SweepComplete: !cappedOut);
    }

    private static string LogoVariant(string rel) =>
        rel == LogoRels.Dark ? LogoRels.Dark : LogoVariants.Default;
}

/// <summary>
/// Re-runs lineup matching over every broadcaster that has no hand-set mapping. Pure database work —
/// no upstream requests — so it is safe to run every night after the lineup refresh, and it is what
/// lets the crawl run before the lineup exists (or a tuner be added later) without re-crawling.
/// </summary>
public record RematchAffiliatesCommand : ICommand<int>;

public class RematchAffiliatesHandler(IWorkspace workspace)
    : ICommandHandler<RematchAffiliatesCommand, int>
{
    public async ValueTask<int> Handle(RematchAffiliatesCommand command, CancellationToken ct)
    {
        var lineup = LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));
        var broadcasters = await workspace.LoadAll<Broadcaster>(ct);

        var matched = 0;
        foreach (var broadcaster in broadcasters)
        {
            if (broadcaster.MapsToBroadcasterId is not null || broadcaster.IptvGuideNumber is { Length: > 0 })
                continue;

            BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
            if (broadcaster.IptvGuideNumber is { Length: > 0 })
                matched++;
        }

        return matched;
    }
}

/// <summary>
/// Re-reads the source record for broadcasters that have never had one, so a logo that only became
/// resolvable later still arrives. Idempotent and bounded by how many are outstanding.
/// </summary>
public record RefreshBroadcasterDetailsCommand : ICommand<int>;

public class RefreshBroadcasterDetailsHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IBroadcasterCatalogSource source,
    ILogger<RefreshBroadcasterDetailsHandler> logger)
    : ICommandHandler<RefreshBroadcasterDetailsCommand, int>
{
    public async ValueTask<int> Handle(RefreshBroadcasterDetailsCommand command, CancellationToken ct)
    {
        var broadcasters = await workspace.Load(new BroadcastersNeedingDetailSpec(), ct);
        var resolved = 0;

        foreach (var broadcaster in broadcasters)
        {
            if (broadcaster.ExternalRefs.ExternalIdFor(source.SourceKey) is not { } externalId)
                continue;

            try
            {
                var detail = await source.GetBroadcasterAsync(externalId, ct);

                if (detail is not null)
                {
                    broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, broadcaster.Kind);

                    foreach (var candidate in detail.Logos ?? [])
                        broadcaster.UpsertLogo(
                            candidate.Rel == LogoRels.Dark ? LogoRels.Dark : LogoVariants.Default,
                            source.SourceKey, candidate.Url, candidate.UpdatedAtUtc);
                }

                broadcaster.MarkDetailFetched();
                resolved++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to resolve broadcaster {Slug}.", broadcaster.Slug);
            }
        }

        if (resolved > 0)
            await unitOfWork.SaveChanges(ct);

        return resolved;
    }
}

/// <summary>
/// Re-pulls teams, colours and logo URLs for every followed league. One request per league, and an
/// unchanged logo costs a conditional GET with no body, so this is cheap enough to run nightly and
/// is how a mid-season rebrand finds its way in without anyone noticing.
/// </summary>
public record RefreshFollowedLeaguesCommand : ICommand<int>;

public class RefreshFollowedLeaguesHandler(
    IReadModel readModel,
    IMediator mediator,
    ILogger<RefreshFollowedLeaguesHandler> logger)
    : ICommandHandler<RefreshFollowedLeaguesCommand, int>
{
    public async ValueTask<int> Handle(RefreshFollowedLeaguesCommand command, CancellationToken ct)
    {
        var leagues = await readModel.Search(new FollowedLeaguesSpec(), ct);
        var refreshed = 0;

        foreach (var league in leagues.Where(l => l.SupportsTeams))
        {
            try
            {
                await mediator.Send(new RefreshLeagueTeamsCommand(league.Id), ct);
                refreshed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to refresh teams for {League}.", league.Slug);
            }
        }

        return refreshed;
    }
}
