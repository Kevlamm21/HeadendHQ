using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters.Specifications;

/// <summary>
/// Broadcasters whose source record has never been read, so their logo is still unresolved.
/// </summary>
public class BroadcastersNeedingDetailSpec : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.DetailFetchedAtUtc == null);
}
