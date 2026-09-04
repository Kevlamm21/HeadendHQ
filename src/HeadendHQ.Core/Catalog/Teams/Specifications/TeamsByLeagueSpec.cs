using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams.Specifications;

public class TeamsByLeagueSpec(int leagueId) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) =>
        queryable.Where(t => t.LeagueId == leagueId).OrderBy(t => t.DisplayName);
}
