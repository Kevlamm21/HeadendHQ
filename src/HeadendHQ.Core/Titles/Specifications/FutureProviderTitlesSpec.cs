using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class FutureProviderTitlesSpec(string provider, DateTime utcNow) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.Provider == provider && t.StartUtc > utcNow);
}
