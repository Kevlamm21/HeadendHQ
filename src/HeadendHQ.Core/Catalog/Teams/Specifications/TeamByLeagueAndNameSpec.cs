using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Teams.Specifications;

public class TeamByLeagueAndNameSpec(int leagueId, string displayName) : ISpecification<Team>
{
    public IQueryable<Team> Apply(IQueryable<Team> queryable) =>
        queryable.Where(t => t.LeagueId == leagueId && t.DisplayName == displayName);
}
