using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Broadcasters.Specifications;

public class BroadcasterBySlugSpec(string slug) : ISpecification<Broadcaster>
{
    public IQueryable<Broadcaster> Apply(IQueryable<Broadcaster> queryable) =>
        queryable.Where(b => b.Slug == slug || b.Aliases.Contains(slug));
}
