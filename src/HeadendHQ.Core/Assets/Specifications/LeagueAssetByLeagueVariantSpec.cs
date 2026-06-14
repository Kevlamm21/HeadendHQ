using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets.Specifications;

public class LeagueAssetByLeagueVariantSpec(League league, string variant) : ISpecification<LeagueAsset>
{
    public IQueryable<LeagueAsset> Apply(IQueryable<LeagueAsset> queryable) =>
        queryable.Where(a => a.League == league && a.Variant == variant);
}
