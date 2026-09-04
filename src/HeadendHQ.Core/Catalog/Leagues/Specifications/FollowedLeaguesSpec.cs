using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Leagues.Specifications;

public class FollowedLeaguesSpec : ISpecification<League>
{
    public IQueryable<League> Apply(IQueryable<League> queryable) => queryable.Where(l => l.IsFollowed);
}
