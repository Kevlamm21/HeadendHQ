using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams.Specifications;

public class TeamByExternalIdSpec(int leagueId, string sourceKey, string externalId) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) =>
        queryable.Where(t => t.LeagueId == leagueId && t.SourceKey == sourceKey && t.ExternalId == externalId);
}
