using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Assets.Specifications;

public class WordMarkByLeagueVariantSpec(League league, string variant) : ISpecification<WordMark>
{
    public IQueryable<WordMark> Apply(IQueryable<WordMark> queryable) =>
        queryable.Where(a => a.League == league && a.Variant == variant);
}
