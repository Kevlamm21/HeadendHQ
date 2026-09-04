using HeadendHQ.Core.Catalog.Broadcasters.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

/// <summary>National networks (no call-sign token), local lineup-matched affiliates, and everything else.</summary>
public enum BroadcasterAffiliateFilter
{
    National,
    Local,
    Other
}

public record GetBroadcastersQuery(bool? SubscribedOnly, BroadcasterAffiliateFilter? Affiliate = null)
    : IQuery<IReadOnlyList<Broadcaster>>;

public class GetBroadcastersHandler(IReadModel readModel)
    : IQueryHandler<GetBroadcastersQuery, IReadOnlyList<Broadcaster>>
{
    public async ValueTask<IReadOnlyList<Broadcaster>> Handle(GetBroadcastersQuery query, CancellationToken ct)
    {
        var broadcasters = query.SubscribedOnly == true
            ? await readModel.Search(new SubscribedBroadcastersSpec(), ct)
            : await readModel.All<Broadcaster>(ct);

        return query.Affiliate switch
        {
            BroadcasterAffiliateFilter.National => [.. broadcasters.Where(b => !b.IsAffiliate)],
            BroadcasterAffiliateFilter.Local => [.. broadcasters.Where(b => b.IsAffiliate && b.IptvGuideNumber is { Length: > 0 })],
            BroadcasterAffiliateFilter.Other => [.. broadcasters.Where(b => b.IsAffiliate && b.IptvGuideNumber is null or "")],
            _ => broadcasters,
        };
    }
}
