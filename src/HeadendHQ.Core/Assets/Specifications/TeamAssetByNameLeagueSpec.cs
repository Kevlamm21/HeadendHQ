using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets.Specifications;

public class TeamAssetByNameLeagueSpec(string teamName, League league) : ISpecification<TeamAsset>
{
    public IQueryable<TeamAsset> Apply(IQueryable<TeamAsset> queryable) =>
        queryable.Where(a => a.TeamName == teamName && a.League == league);
}
