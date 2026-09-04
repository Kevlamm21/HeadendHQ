using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Leagues.Specifications;

public class LeagueByExternalIdSpec(string sourceKey, string externalId) : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) =>
        queryable.Where(l => l.SourceKey == sourceKey && l.ExternalId == externalId);
}
