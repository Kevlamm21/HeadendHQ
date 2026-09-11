using HeadendHQ.Core.Catalog.Sports.CommandHandlers;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Data;

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
