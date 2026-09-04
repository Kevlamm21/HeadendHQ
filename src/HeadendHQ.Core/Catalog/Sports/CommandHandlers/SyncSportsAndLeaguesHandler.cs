using HeadendHQ.Core.Catalog.Leagues.Specifications;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Catalog.Sports.Specifications;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Sports.CommandHandlers;

/// <summary>
/// Walks the source's sport and league catalog and mirrors it into the database.
/// <para>
/// Only names are recorded — no artwork at all. ESPN publishes 356 leagues, and a logo row now means
/// bytes on disk, so recording every candidate would mean thousands of downloads for leagues nobody
/// follows. Artwork arrives when a league is actually followed, via <see cref="RefreshLeagueLogosCommand"/>.
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
    ISportsCatalogSource source,
    ILogger<SyncSportsAndLeaguesHandler> logger)
    : ICommandHandler<SyncSportsAndLeaguesCommand, CatalogSyncState>
{
    public async ValueTask<CatalogSyncState> Handle(SyncSportsAndLeaguesCommand command, CancellationToken ct)
    {
        var state = await workspace.LoadSingleOrDefault(new CatalogSyncStateSpec(), ct)
            ?? throw new InvalidOperationException("CatalogSyncState not found.");

        state.Begin();
        state.EnterStage("Sports");
        await unitOfWork.SaveChanges(ct);

        try
        {
            var sports = await source.GetSportsAsync(ct);
            logger.LogInformation("Catalog sync: {Count} sport(s) from {Source}.", sports.Count, source.SourceKey);

            foreach (var descriptor in sports)
            {
                // Every sport is recorded whether or not we walk it, so a sport outside the
                // discovery set can still be pulled on demand later without a second discovery of
                // the sport list itself.
                var sport = await UpsertSportAsync(descriptor, ct);

                if (!SourceSettings.CoversSport(descriptor.Slug))
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
        }

        logger.LogInformation("Catalog sync: {Count} league(s) for {Sport}.", leagues.Count, sportSlug);
    }
}
