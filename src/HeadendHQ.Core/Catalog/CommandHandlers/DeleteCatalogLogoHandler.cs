using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.CommandHandlers;

public enum CatalogLogoOwner
{
    Team,
    League,
    Broadcaster
}

public record DeleteCatalogLogoCommand(CatalogLogoOwner Owner, int OwnerId, int LogoId) : ICommand<int>;

public class DeleteCatalogLogoHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IMediator mediator)
    : ICommandHandler<DeleteCatalogLogoCommand, int>
{
    public async ValueTask<int> Handle(DeleteCatalogLogoCommand command, CancellationToken ct)
    {
        var imageId = command.Owner switch
        {
            CatalogLogoOwner.Team =>
                (await workspace.LoadById<Team, int>(command.OwnerId, ct)).RemoveLogo(command.LogoId),
            CatalogLogoOwner.League =>
                (await workspace.LoadById<League, int>(command.OwnerId, ct)).RemoveLogo(command.LogoId),
            CatalogLogoOwner.Broadcaster =>
                (await workspace.LoadById<Broadcaster, int>(command.OwnerId, ct)).RemoveLogo(command.LogoId),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Owner, null),
        };

        await unitOfWork.SaveChanges(ct);

        return await mediator.Send(new DeleteOrphanedImagesCommand([imageId]), ct);
    }
}
