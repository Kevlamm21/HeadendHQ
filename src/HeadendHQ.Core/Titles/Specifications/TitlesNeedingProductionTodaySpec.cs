using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitlesNeedingProductionTodaySpec : ISpecification<Title>
{
    private readonly DateTime _todayUtcStart;
    private readonly DateTime _todayUtcEnd;

    public TitlesNeedingProductionTodaySpec()
    {
        (_todayUtcStart, _todayUtcEnd) = LocalDay.UtcWindow();
    }

    public IQueryable<Title> Apply(IQueryable<Title> q)
    {
        var composed = TitleProductionProfile.ComposedArtworkTypes;

        return q.Where(t => t.IsActive &&
            (!t.IsVideoCreated || (!t.ArtworkCreated && composed.Contains(t.Type))) &&
            (t.StartUtc == null || (t.StartUtc >= _todayUtcStart && t.StartUtc < _todayUtcEnd)));
    }
}
