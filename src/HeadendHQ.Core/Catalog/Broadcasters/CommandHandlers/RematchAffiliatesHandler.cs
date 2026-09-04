using HeadendHQ.Core.Iptv;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;

/// <summary>
/// Re-runs lineup matching over every broadcaster that has no hand-set mapping. Pure database work —
/// no upstream requests — so it is safe to run every night after the lineup refresh, and it is what
/// lets the crawl run before the lineup exists (or a tuner be added later) without re-crawling.
/// </summary>
public record RematchAffiliatesCommand : ICommand<int>;

public class RematchAffiliatesHandler(IWorkspace workspace)
    : ICommandHandler<RematchAffiliatesCommand, int>
{
    public async ValueTask<int> Handle(RematchAffiliatesCommand command, CancellationToken ct)
    {
        var lineup = LineupIndex.Build(await workspace.Load(AllSpecification<IptvChannel>.Instance, ct));
        var broadcasters = await workspace.LoadAll<Broadcaster>(ct);

        var matched = 0;
        foreach (var broadcaster in broadcasters)
        {
            if (broadcaster.MapsToBroadcasterId is not null || broadcaster.IptvGuideNumber is { Length: > 0 })
                continue;

            BroadcasterClassification.ClassifyAgainstLineup(broadcaster, lineup);
            if (broadcaster.IptvGuideNumber is { Length: > 0 })
                matched++;
        }

        return matched;
    }
}
