using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitleByNameStartSpec(string name, DateTime? startUtc) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q)
    {
        q = q.Where(t => t.Name == name);
        return startUtc.HasValue ? q.Where(t => t.StartUtc == startUtc.Value) : q;
    }
}
