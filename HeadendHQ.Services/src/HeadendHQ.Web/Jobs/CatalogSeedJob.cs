using HeadendHQ.Data;
using Mediator;

namespace HeadendHQ.Web.Jobs;

public class CatalogSeedJob(IMediator mediator, ILogger<CatalogSeedJob> logger)
{
    public const string JobName = "catalog-seed";

    public Task RunAsync(CancellationToken ct) => CatalogSeeder.SeedAsync(mediator, logger, ct);
}
