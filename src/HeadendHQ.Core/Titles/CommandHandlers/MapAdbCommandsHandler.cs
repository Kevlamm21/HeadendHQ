using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record MapAdbCommandsCommand : ICommand<Unit>;

public class MapAdbCommandsHandler(AdbMappingService adbMapper)
    : ICommandHandler<MapAdbCommandsCommand, Unit>
{
    public async ValueTask<Unit> Handle(MapAdbCommandsCommand command, CancellationToken ct)
    {
        await adbMapper.MapPendingAsync(ct);
        return Unit.Value;
    }
}
