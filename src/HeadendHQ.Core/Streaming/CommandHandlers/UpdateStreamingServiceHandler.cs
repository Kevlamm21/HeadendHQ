using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Streaming.CommandHandlers;

public record UpdateStreamingServiceCommand(
    int StreamingServiceId,
    bool? IsEnabled,
    bool SetLogoBroadcaster = false,
    int? LogoBroadcasterId = null) : ICommand<StreamingService>;

public class UpdateStreamingServiceHandler(IWorkspace workspace)
    : ICommandHandler<UpdateStreamingServiceCommand, StreamingService>
{
    public async ValueTask<StreamingService> Handle(UpdateStreamingServiceCommand command, CancellationToken ct)
    {
        var service = await workspace.LoadById<StreamingService, int>(command.StreamingServiceId, ct);

        if (command.IsEnabled is { } enabled)
            service.Enable(enabled);

        if (command.SetLogoBroadcaster)
        {
            if (command.LogoBroadcasterId is { } broadcasterId)
                _ = await workspace.LoadById<Broadcaster, int>(broadcasterId, ct);

            service.BorrowLogoFrom(command.LogoBroadcasterId);
        }

        return service;
    }
}
