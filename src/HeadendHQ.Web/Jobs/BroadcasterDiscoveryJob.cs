using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Jobs;

public class BroadcasterDiscoveryJob(IMediator mediator, ILogger<BroadcasterDiscoveryJob> logger)
{
    public const string JobName = "broadcaster-discovery";

    public Task RunAsync(CancellationToken ct) => RunAsync(null, false, ct);

    public Task RunAsync(int? max, CancellationToken ct) => RunAsync(max, false, ct);

    public async Task RunAsync(int? max, bool refresh, CancellationToken ct)
    {
        var result = await mediator.Send(new DiscoverBroadcastersCommand(max, refresh), ct);
        logger.LogInformation(
            "Broadcaster discovery finished: examined {Examined}, created {Created}, complete={Complete}.",
            result.Examined, result.Created, result.SweepComplete);
    }
}
