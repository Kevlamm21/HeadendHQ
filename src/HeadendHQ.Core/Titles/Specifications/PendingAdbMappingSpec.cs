using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class PendingAdbMappingSpec : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.AdbCommand == null && t.EventUrl != null);
}
