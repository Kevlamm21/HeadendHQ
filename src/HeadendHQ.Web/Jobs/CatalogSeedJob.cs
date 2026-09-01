using HeadendHQ.Data;
using Mediator;

namespace HeadendHQ.Web.Jobs;

/// <summary>
/// Runs the one-time catalog seed off the startup path.
/// <para>
/// Enqueued rather than awaited during startup: seeding needs the network and takes minutes, and a
/// container whose health check is blocked on an upstream API looks like a failed deploy. The
/// "once per database" guarantee lives in the seeder's persisted marker, not here, so enqueueing it
/// on every boot is safe.
/// </para>
/// </summary>
public class CatalogSeedJob(IMediator mediator, ILogger<CatalogSeedJob> logger)
{
    public const string JobName = "catalog-seed";

    public Task RunAsync(CancellationToken ct) => CatalogSeeder.SeedAsync(mediator, logger, ct);
}
