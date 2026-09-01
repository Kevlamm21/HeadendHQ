using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

/// <summary>
/// Matches a title by its name and kickoff. Titles no longer carry a provider or an external id —
/// whatever produced one owns that identity — so name plus start time is the natural key for
/// spotting a duplicate.
/// </summary>
public class TitleByNameStartSpec(string name, DateTime? startUtc) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q)
    {
        q = q.Where(t => t.Name == name);
        return startUtc.HasValue ? q.Where(t => t.StartUtc == startUtc.Value) : q;
    }
}
