namespace HeadendHQ.Core.Titles.Specifications;

public class TitleByIdSpec(Guid id) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.Id == id);
}
