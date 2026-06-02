namespace HeadendHQ.Core.Titles.Specifications;

public class TitleByNameWithVideoSpec(string name) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.Name.ToLower() == name.ToLower() && t.DummyVideoPath != null)
         .OrderBy(t => t.StartUtc);
}
