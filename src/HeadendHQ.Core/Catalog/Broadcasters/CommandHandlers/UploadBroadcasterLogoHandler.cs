using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record UploadBroadcasterLogoCommand(int BroadcasterId, byte[] Bytes) : ICommand<Broadcaster>;

public class UploadBroadcasterLogoHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadBroadcasterLogoCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(UploadBroadcasterLogoCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.BroadcasterLogo), ct);

        // Under the label the preference chain reaches for first, so an upload actually displaces
        // whatever ESPN supplied rather than sitting behind it.
        broadcaster.UpsertLogo(LogoRels.Dark, imageId, ImageOrigin.Manual);

        return broadcaster;
    }
}
