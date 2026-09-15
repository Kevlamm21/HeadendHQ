using HeadendHQ.Core.Catalog.Broadcasters.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

public record GetBroadcastersQuery(bool? SubscribedOnly) : IQuery<IReadOnlyList<Broadcaster>>;

public class GetBroadcastersHandler(IReadModel readModel)
    : IQueryHandler<GetBroadcastersQuery, IReadOnlyList<Broadcaster>>
{
    public async ValueTask<IReadOnlyList<Broadcaster>> Handle(GetBroadcastersQuery query, CancellationToken ct) =>
        query.SubscribedOnly == true
            ? await readModel.Search(new SubscribedBroadcastersSpec(), ct)
            : await readModel.All<Broadcaster>(ct);
}
