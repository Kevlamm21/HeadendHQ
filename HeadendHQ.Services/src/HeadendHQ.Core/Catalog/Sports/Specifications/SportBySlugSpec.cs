using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog.Sports.Specifications;

public class SportBySlugSpec(string slug) : ISpecification<Sport>
{
    public IQueryable<Sport> Apply(IQueryable<Sport> queryable) => queryable.Where(s => s.Slug == slug);
}
