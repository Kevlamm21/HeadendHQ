namespace HeadendHQ.Core.Titles.Specifications;

public class TitleByProviderExternalIdSpec(string provider, string externalId) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.Provider == provider && t.ExternalId == externalId);
}
