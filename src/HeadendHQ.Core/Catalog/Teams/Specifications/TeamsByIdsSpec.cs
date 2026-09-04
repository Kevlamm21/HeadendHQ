using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams.Specifications;

public class TeamsByIdsSpec(IReadOnlyCollection<int> ids) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) => queryable.Where(t => ids.Contains(t.Id));
}
