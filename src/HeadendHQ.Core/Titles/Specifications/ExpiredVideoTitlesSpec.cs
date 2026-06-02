namespace HeadendHQ.Core.Titles.Specifications;

public class ExpiredVideoTitlesSpec(DateTime cutoff) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.DummyVideoPath != null && t.EndUtc.HasValue && t.EndUtc < cutoff);
}
