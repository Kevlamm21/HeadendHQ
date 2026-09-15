using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Streaming.CommandHandlers;

public record UploadStreamingServiceLogoCommand(int StreamingServiceId, string Variant, byte[] Bytes)
    : ICommand<StreamingService>;

public class UploadStreamingServiceLogoHandler(IWorkspace workspace, IUnitOfWork unitOfWork, IMediator mediator)
    : ICommandHandler<UploadStreamingServiceLogoCommand, StreamingService>
{
    public async ValueTask<StreamingService> Handle(UploadStreamingServiceLogoCommand command, CancellationToken ct)
    {
        var service = await workspace.LoadById<StreamingService, int>(command.StreamingServiceId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.BroadcasterLogo), ct);

        if (service.SetLogo(command.Variant, imageId) is { } replaced)
        {
            await unitOfWork.SaveChanges(ct);
            await mediator.Send(new DeleteOrphanedImagesCommand([replaced]), ct);
        }

        return service;
    }
}
