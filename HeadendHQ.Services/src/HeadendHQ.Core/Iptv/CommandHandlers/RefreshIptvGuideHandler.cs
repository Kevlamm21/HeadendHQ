using Mediator;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record RefreshIptvGuideCommand : ICommand<Unit>;

public class RefreshIptvGuideHandler(IIptvService service) : ICommandHandler<RefreshIptvGuideCommand, Unit>
{
    public async ValueTask<Unit> Handle(RefreshIptvGuideCommand command, CancellationToken ct)
    {
        await service.RefreshGuideAsync(ct);
        return Unit.Value;
    }
}
