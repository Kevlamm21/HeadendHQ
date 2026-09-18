using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams.Specifications;

public class FollowedTeamsSpec : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) => queryable.Where(t => t.IsFollowed);
}
