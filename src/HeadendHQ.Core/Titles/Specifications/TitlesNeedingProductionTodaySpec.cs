using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitlesNeedingProductionTodaySpec : ISpecification<Title>
{
    private readonly DateTime _todayUtcStart;
    private readonly DateTime _todayUtcEnd;

    public TitlesNeedingProductionTodaySpec()
    {
        _todayUtcStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.Date, TimeZoneInfo.Local);
        _todayUtcEnd = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.Date.AddDays(1), TimeZoneInfo.Local);
    }

    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.IsActive &&
            (t.VodLauncherPath == null || !t.ArtworkCreated) &&
            (t.StartUtc == null || (t.StartUtc >= _todayUtcStart && t.StartUtc < _todayUtcEnd)));
}
