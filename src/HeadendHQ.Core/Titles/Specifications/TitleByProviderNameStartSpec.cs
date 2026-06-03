using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitleByProviderNameStartSpec(string provider, string name, DateTime? startUtc) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q)
    {
        q = q.Where(t => t.Provider == provider && t.Name == name);
        if (startUtc.HasValue) q = q.Where(t => t.StartUtc == startUtc.Value);
        return q;
    }
}
