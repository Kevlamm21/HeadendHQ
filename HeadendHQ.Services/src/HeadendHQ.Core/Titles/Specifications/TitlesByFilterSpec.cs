using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitlesByFilterSpec(DateTime? from, DateTime? to, TitleType? type) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q)
    {
        if (from.HasValue) q = q.Where(t => t.StartUtc >= from.Value);
        if (to.HasValue) q = q.Where(t => t.StartUtc <= to.Value);
        if (type.HasValue) q = q.Where(t => t.Type == type.Value);
        return q.OrderBy(t => t.StartUtc);
    }
}
