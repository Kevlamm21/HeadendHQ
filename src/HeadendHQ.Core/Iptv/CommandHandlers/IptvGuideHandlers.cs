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

public record GetIptvGuideQuery : IQuery<string?>;

public class GetIptvGuideHandler(IIptvService service) : IQueryHandler<GetIptvGuideQuery, string?>
{
    public async ValueTask<string?> Handle(GetIptvGuideQuery query, CancellationToken ct) =>
        await service.GetGuideContentAsync(ct);
}
