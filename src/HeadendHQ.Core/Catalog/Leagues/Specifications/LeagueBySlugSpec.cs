using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Leagues.Specifications;

public class LeagueBySlugSpec(string slug) : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) => queryable.Where(l => l.Slug == slug);
}
