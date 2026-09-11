using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Iptv.Specifications;

public class ProgrammesOnChannelSpec(string guideNumber, DateTime fromUtc, DateTime toUtc)
    : ISpecification<IptvProgramme>
{
    public IQueryable<IptvProgramme> Apply(IQueryable<IptvProgramme> queryable) =>
        queryable
            .Where(p => p.GuideNumber == guideNumber && p.StartUtc < toUtc && p.StopUtc > fromUtc)
            .OrderBy(p => p.StartUtc);
}
