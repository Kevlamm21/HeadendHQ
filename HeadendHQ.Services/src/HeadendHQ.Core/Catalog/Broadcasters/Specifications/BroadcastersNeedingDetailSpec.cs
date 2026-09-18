using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters.Specifications;

public class BroadcastersNeedingDetailSpec : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.DetailFetchedAtUtc == null);
}
