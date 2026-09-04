using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Specifications;

public class CatalogSyncStateSpec : ISpecification<CatalogSyncState>
{
    public IQueryable<CatalogSyncState> Apply(IQueryable<CatalogSyncState> queryable) => queryable;
}
