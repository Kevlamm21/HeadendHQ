using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record UploadBroadcasterLogoCommand(int BroadcasterId, byte[] Bytes) : ICommand<BroadcasterLogo>;

public class UploadBroadcasterLogoHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadBroadcasterLogoCommand, BroadcasterLogo>
{
    public async ValueTask<BroadcasterLogo> Handle(UploadBroadcasterLogoCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.BroadcasterLogo), ct);

        return broadcaster.AddUploadedLogo(imageId);
    }
}
