using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Streaming.CommandHandlers;

public record DeleteStreamingServiceLogoCommand(int StreamingServiceId, string Variant) : ICommand<int>;

public class DeleteStreamingServiceLogoHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IMediator mediator)
    : ICommandHandler<DeleteStreamingServiceLogoCommand, int>
{
    public async ValueTask<int> Handle(DeleteStreamingServiceLogoCommand command, CancellationToken ct)
    {
        var service = await workspace.LoadById<StreamingService, int>(command.StreamingServiceId, ct);

        if (service.RemoveLogo(command.Variant) is not { } imageId)
            return 0;

        await unitOfWork.SaveChanges(ct);

        return await mediator.Send(new DeleteOrphanedImagesCommand([imageId]), ct);
    }
}
