using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Streaming.CommandHandlers;

public record GetStreamingServicesQuery : IQuery<IReadOnlyList<StreamingService>>;

public class GetStreamingServicesHandler(IReadModel readModel)
    : IQueryHandler<GetStreamingServicesQuery, IReadOnlyList<StreamingService>>
{
    public async ValueTask<IReadOnlyList<StreamingService>> Handle(GetStreamingServicesQuery query, CancellationToken ct) =>
        [.. (await readModel.All<StreamingService>(ct)).OrderBy(s => s.Kind).ThenBy(s => s.Key)];
}
