using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record UpdateBroadcasterCommand(
    int BroadcasterId,
    bool? Subscribed,
    bool SetMapping = false,
    int? MapsToBroadcasterId = null,
    string? IptvGuideNumber = null,
    int? SelectedLogoId = null) : ICommand<Broadcaster>;

public class UpdateBroadcasterHandler(IWorkspace workspace)
    : ICommandHandler<UpdateBroadcasterCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(UpdateBroadcasterCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        if (command.Subscribed is { } subscribed) broadcaster.Subscribe(subscribed);

        if (command.SetMapping)
        {
            if (command.MapsToBroadcasterId is { } targetId)
                _ = await workspace.LoadById<Broadcaster, int>(targetId, ct);

            broadcaster.SetMapping(command.MapsToBroadcasterId, command.IptvGuideNumber);
        }

        if (command.SelectedLogoId is { } logoId)
            broadcaster.SelectLogo(logoId);

        return broadcaster;
    }
}
