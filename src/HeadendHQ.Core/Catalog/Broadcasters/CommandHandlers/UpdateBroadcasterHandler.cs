using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

/// <summary>
/// <paramref name="SetMapping"/> is what distinguishes "leave the mapping alone" from "clear it":
/// both targets are nullable, so their absence alone cannot say which was meant.
/// </summary>
public record UpdateBroadcasterCommand(
    int BroadcasterId,
    bool? Subscribed,
    bool SetMapping = false,
    int? MapsToBroadcasterId = null,
    string? IptvGuideNumber = null) : ICommand<Broadcaster>;

public class UpdateBroadcasterHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UpdateBroadcasterCommand, Broadcaster>
{
    public async ValueTask<Broadcaster> Handle(UpdateBroadcasterCommand command, CancellationToken ct)
    {
        var broadcaster = await workspace.LoadById<Broadcaster, int>(command.BroadcasterId, ct);

        if (command.Subscribed is { } subscribed) broadcaster.Subscribe(subscribed);

        if (command.SetMapping)
        {
            // Loaded rather than trusted: pointing at a broadcaster that does not exist would only
            // surface much later, as a title produced with no launch target at all.
            if (command.MapsToBroadcasterId is { } targetId)
                _ = await workspace.LoadById<Broadcaster, int>(targetId, ct);

            broadcaster.SetMapping(command.MapsToBroadcasterId, command.IptvGuideNumber);
        }

        // Subscribing is what makes a network's mark worth holding — the crawl deliberately skips
        // artwork for the ~1300 it walks, most of which never reach a poster.
        if (broadcaster.IsSubscribed)
            await mediator.Send(new RefreshBroadcasterLogosCommand(broadcaster.Id), ct);

        return broadcaster;
    }
}
