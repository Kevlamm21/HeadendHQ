using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitlesNeedingVideoTodaySpec(DateTime todayUtcStart, DateTime todayUtcEnd) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.DummyVideoPath == null && t.StartUtc >= todayUtcStart && t.StartUtc < todayUtcEnd);
}
