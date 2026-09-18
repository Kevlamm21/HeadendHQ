using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters.Specifications;

public class SubscribedBroadcastersSpec : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) => queryable.Where(b => b.IsSubscribed);
}
