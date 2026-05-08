using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record MapAdbCommandsCommand : ICommand<Unit>;

public class MapAdbCommandsHandler(IAdbMappingService adbMapper)
    : ICommandHandler<MapAdbCommandsCommand, Unit>
{
    public async ValueTask<Unit> Handle(MapAdbCommandsCommand command, CancellationToken ct)
    {
        await adbMapper.MapPendingAsync(ct);
        return Unit.Value;
    }
}
