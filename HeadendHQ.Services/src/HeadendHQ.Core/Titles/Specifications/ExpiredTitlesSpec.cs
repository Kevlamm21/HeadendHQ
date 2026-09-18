using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class ExpiredTitlesSpec(DateTime cutoff) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.EndUtc < cutoff || (t.EndUtc == null && t.StartUtc < cutoff));
}
