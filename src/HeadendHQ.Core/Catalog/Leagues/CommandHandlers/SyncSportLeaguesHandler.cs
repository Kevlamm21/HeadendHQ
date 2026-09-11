using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports.Specifications;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record SyncSportLeaguesCommand(string SportSlug) : ICommand<int>;

public class SyncSportLeaguesHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    IMediator mediator,
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
            var descriptor = (await source.GetSportsAsync(ct))
                .FirstOrDefault(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotFoundException<Sport>(slug);

            sport = new Sport(descriptor.Slug, descriptor.Name);
            sport.TrackSource(source.SourceKey, descriptor.ExternalId);
            workspace.Add(sport);
            await unitOfWork.SaveChanges(ct);
        }

        var leagues = await source.GetLeaguesAsync(sport.Slug, ct);

        await LeagueCatalog.UpsertAsync(workspace, mediator, sport, source.SourceKey, leagues, ct);

        await unitOfWork.SaveChanges(ct);
        logger.LogInformation("Pulled {Count} league(s) for {Sport} on request.", leagues.Count, sport.Slug);

        return leagues.Count;
    }
}
