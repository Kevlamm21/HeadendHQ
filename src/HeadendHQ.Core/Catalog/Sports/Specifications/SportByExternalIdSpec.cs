using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Sports.Specifications;

public class SportByExternalIdSpec(string sourceKey, string externalId) : ISpecification<Sport>
{
    public IQueryable<Sport> Apply(IQueryable<Sport> queryable) =>
        queryable.Where(s => s.SourceKey == sourceKey && s.ExternalId == externalId);
}
