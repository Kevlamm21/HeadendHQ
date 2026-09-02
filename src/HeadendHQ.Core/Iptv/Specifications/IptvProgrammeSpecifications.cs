using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Iptv.Specifications;

/// <summary>
/// Everything airing on one channel that overlaps a window. Overlap rather than containment, because
/// the question being asked is "is this game on that channel around then" and a three-hour programme
/// starting before the window still answers it.
/// </summary>
public class ProgrammesOnChannelSpec(string guideNumber, DateTime fromUtc, DateTime toUtc)
    : ISpecification<IptvProgramme>
{
    public IQueryable<IptvProgramme> Apply(IQueryable<IptvProgramme> queryable) =>
        queryable
            .Where(p => p.GuideNumber == guideNumber && p.StartUtc < toUtc && p.StopUtc > fromUtc)
            .OrderBy(p => p.StartUtc);
}
