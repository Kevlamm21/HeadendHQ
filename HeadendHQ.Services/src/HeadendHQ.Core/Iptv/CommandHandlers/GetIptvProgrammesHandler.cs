using HeadendHQ.Core.Iptv.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record GetIptvProgrammesQuery(string GuideNumber, DateTime? FromUtc, DateTime? ToUtc)
    : IQuery<IReadOnlyList<IptvProgramme>>;

public class GetIptvProgrammesHandler(IReadModel readModel)
    : IQueryHandler<GetIptvProgrammesQuery, IReadOnlyList<IptvProgramme>>
{
    public async ValueTask<IReadOnlyList<IptvProgramme>> Handle(GetIptvProgrammesQuery query, CancellationToken ct)
    {
        var from = query.FromUtc ?? DateTime.UtcNow;
        var to = query.ToUtc ?? from.AddDays(1);

        return await readModel.Search(new ProgrammesOnChannelSpec(query.GuideNumber, from, to), ct);
    }
}
