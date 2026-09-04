using Mediator;

namespace HeadendHQ.Core.Iptv.CommandHandlers;

public record GetIptvGuideQuery : IQuery<string?>;

public class GetIptvGuideHandler(IIptvService service) : IQueryHandler<GetIptvGuideQuery, string?>
{
    public async ValueTask<string?> Handle(GetIptvGuideQuery query, CancellationToken ct) =>
        await service.GetGuideContentAsync(ct);
}
