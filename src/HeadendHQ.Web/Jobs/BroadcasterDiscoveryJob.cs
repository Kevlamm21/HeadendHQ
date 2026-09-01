using HeadendHQ.Core.Catalog.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Jobs;

/// <summary>
/// Runs the ESPN media-index crawl as its own Hangfire job, so it gets its own DI scope — and
/// therefore its own upstream request budget — rather than sharing the nightly scrape's.
/// <para>
/// Enqueued once on boot while the completion marker is unset (a fresh database, or a crawl that was
/// interrupted), and again each night to pick up networks ESPN adds. A completed crawl is two
/// index requests and stops.
/// </para>
/// </summary>
public class BroadcasterDiscoveryJob(IMediator mediator, ILogger<BroadcasterDiscoveryJob> logger)
{
    public const string JobName = "broadcaster-discovery";

    public Task RunAsync(CancellationToken ct) => RunAsync(null, ct);

    public async Task RunAsync(int? max, CancellationToken ct)
    {
        var result = await mediator.Send(new DiscoverBroadcastersCommand(max), ct);
        logger.LogInformation(
            "Broadcaster discovery finished: examined {Examined}, created {Created}, complete={Complete}.",
            result.Examined, result.Created, result.SweepComplete);
    }
}
