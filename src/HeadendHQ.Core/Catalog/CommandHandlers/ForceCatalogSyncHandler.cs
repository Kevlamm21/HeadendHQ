using HeadendHQ.Core.Catalog.Specifications;
using HeadendHQ.Core.Catalog.Sports.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

/// <summary>
/// Clears the completion marker and walks the catalog again. Existing rows are matched by slug and
/// updated in place, so a re-sync corrects drift without duplicating anything.
/// </summary>
public record ForceCatalogSyncCommand : ICommand<CatalogSyncState>;

public class ForceCatalogSyncHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IMediator mediator)
    : ICommandHandler<ForceCatalogSyncCommand, CatalogSyncState>
{
    public async ValueTask<CatalogSyncState> Handle(ForceCatalogSyncCommand command, CancellationToken ct)
    {
        var state = await workspace.LoadSingleOrDefault(new CatalogSyncStateSpec(), ct)
            ?? throw new InvalidOperationException("CatalogSyncState not found.");

        state.Reset();
        await unitOfWork.SaveChanges(ct);

        return await mediator.Send(new SyncSportsAndLeaguesCommand(), ct);
    }
}
