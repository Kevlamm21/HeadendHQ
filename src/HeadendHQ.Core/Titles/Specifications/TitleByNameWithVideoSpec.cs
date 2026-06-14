using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitleByNameWithVideoSpec(string name) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.Name.ToLower() == name.ToLower() && t.VodLauncherPath != null)
         .OrderBy(t => t.StartUtc);
}
