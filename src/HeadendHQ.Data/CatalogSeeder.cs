using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Catalog.Sports.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Data;

/// <summary>
/// Fills a newly created database with the source's sport and league catalog.
/// <para>
/// This is seeding rather than a scheduled job: it runs once per database and never again. The
/// guarantee is the persisted <c>CatalogSyncState</c> marker, not a schedule — a restart, a
/// debugging session or a redeploy against an existing volume all find the marker set and do
/// nothing. It is still checkpointed per sport, so an interrupted first run resumes where it stopped
/// instead of starting over.
/// </para>
/// <para>
/// It is deliberately <em>not</em> called from <see cref="DbExtensions.InitializeDatabase"/>: it
/// needs the network and takes minutes, and a container whose health check is waiting on an upstream
/// API looks like a failed deploy. The caller runs it in the background once the app is serving.
/// </para>
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(IMediator mediator, ILogger logger, CancellationToken ct = default)
    {
        var state = await mediator.Send(new GetCatalogSyncStateQuery(), ct);

        if (state is null)
        {
            logger.LogWarning("No catalog sync state row; skipping catalog seed.");
            return;
        }

        if (!state.NeedsInitialDiscovery)
        {
            logger.LogInformation(
                "Catalog already seeded {When:u}; nothing to do.", state.InitialDiscoveryCompletedAtUtc);
            return;
        }

        logger.LogInformation("Seeding the catalog. This runs once and takes a few minutes.");

        try
        {
            var result = await mediator.Send(new SyncSportsAndLeaguesCommand(), ct);
            logger.LogInformation("Catalog seed finished with stage {Stage}.", result.Stage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Resumable by design: the completion marker stays unset, so the next start picks up
            // from the last finished sport.
            logger.LogError(ex, "Catalog seed failed; it will resume on the next start.");
        }
    }
}
