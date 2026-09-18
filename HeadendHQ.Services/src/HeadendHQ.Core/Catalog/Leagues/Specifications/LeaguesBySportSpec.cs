using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Leagues.Specifications;

public class LeaguesBySportSpec(int sportId) : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) => queryable.Where(l => l.SportId == sportId);
}
