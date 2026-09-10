using HeadendHQ.Core.Catalog.Sports.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Data;

/// <summary>
/// Fills a newly created database with the source's sport and league catalog.
/// <para>
/// This is seeding rather than a scheduled job: it runs once, when the database is first created.
/// The caller (<see cref="HeadendHQ.Web.Infrastructure"/> <c>UseJobs</c>) only enqueues it on a
/// fresh database, so a restart, a debugging session or a redeploy against an existing volume never
/// reach here. There is no persisted marker and no per-boot check.
/// </para>
/// <para>
/// It is deliberately run in the background rather than awaited during startup: it needs the network
/// and takes minutes, and a container whose health check is waiting on an upstream API looks like a
/// failed deploy. If it fails partway there is no automatic retry — recovery is a manual
/// <c>POST /catalog/sync</c>.
/// </para>
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(IMediator mediator, ILogger logger, CancellationToken ct = default)
    {
        logger.LogInformation("Seeding the catalog. This runs once and takes a few minutes.");

        try
        {
            var result = await mediator.Send(new SyncSportsAndLeaguesCommand(), ct);
            logger.LogInformation(
                "Catalog seed finished: {Sports} sport(s) examined, {Leagues} league(s) upserted.",
                result.SportsExamined, result.LeaguesUpserted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Catalog seed failed; trigger POST /catalog/sync to retry.");
        }
    }
}
