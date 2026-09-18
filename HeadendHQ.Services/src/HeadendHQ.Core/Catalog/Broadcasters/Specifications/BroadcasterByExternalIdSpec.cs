using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters.Specifications;

public class BroadcasterByExternalIdSpec(string sourceKey, string externalId) : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.SourceKey == sourceKey && b.ExternalId == externalId);
}
