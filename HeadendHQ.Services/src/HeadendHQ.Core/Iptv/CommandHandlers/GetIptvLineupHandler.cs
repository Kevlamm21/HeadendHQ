using Mediator;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record GetIptvLineupQuery : IQuery<IReadOnlyList<IptvChannel>>;

public class GetIptvLineupHandler(IIptvService service) : IQueryHandler<GetIptvLineupQuery, IReadOnlyList<IptvChannel>>
{
    public async ValueTask<IReadOnlyList<IptvChannel>> Handle(GetIptvLineupQuery query, CancellationToken ct) =>
        await service.GetLineupAsync(ct);
}
