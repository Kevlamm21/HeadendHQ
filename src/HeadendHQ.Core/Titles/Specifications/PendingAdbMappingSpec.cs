using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class PendingAdbMappingSpec(DateTime nowUtc) : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.IsActive &&
            t.AdbCommand == null &&
            t.EventUrl != null &&
            t.StartUtc != null &&
            t.StartUtc > nowUtc);
}
