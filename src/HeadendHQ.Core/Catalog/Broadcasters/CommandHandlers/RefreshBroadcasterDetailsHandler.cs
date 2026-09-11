using HeadendHQ.Core.Catalog.Broadcasters.Specifications;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

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
            if (broadcaster.ExternalIdFor(source.SourceKey) is not { } externalId)
                continue;

            try
            {
                var detail = await source.GetBroadcasterAsync(externalId, ct);

                if (detail is not null)
                    broadcaster.Describe(detail.Name, detail.ShortName, detail.CallLetters, broadcaster.Kind);

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
