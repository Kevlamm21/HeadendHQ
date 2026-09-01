using Mediator;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record RefreshIptvLineupCommand : ICommand<Unit>;

public class RefreshIptvLineupHandler(IIptvService service) : ICommandHandler<RefreshIptvLineupCommand, Unit>
{
    public async ValueTask<Unit> Handle(RefreshIptvLineupCommand command, CancellationToken ct)
    {
        await service.RefreshLineupAsync(ct);
        return Unit.Value;
    }
}

public record GetIptvLineupQuery : IQuery<IReadOnlyList<IptvChannel>>;

public class GetIptvLineupHandler(IIptvService service) : IQueryHandler<GetIptvLineupQuery, IReadOnlyList<IptvChannel>>
{
    public async ValueTask<IReadOnlyList<IptvChannel>> Handle(GetIptvLineupQuery query, CancellationToken ct) =>
        await service.GetLineupAsync(ct);
}
