using Hangfire;
using HeadendHQ.Core.Events;
using HeadendHQ.Core.Events.CommandHandlers;
using Mediator;

namespace HeadendHQ.Hangfire;

/// <summary>
/// Runs each event's detail lookup as its own background job.
/// <para>
/// Splitting it out keeps the scrape short and lets one unreadable game retry on its own instead of
/// taking the run with it, and each event is looked up once ever rather than every night.
/// </para>
/// </summary>
public class EventDetailQueue(IBackgroundJobClient jobClient) : IEventDetailQueue
{
    public void Enqueue(Guid sportingEventId) =>
        jobClient.Enqueue<EventDetailJob>(job => job.RunAsync(sportingEventId, CancellationToken.None));
}

public class EventDetailJob(IMediator mediator)
{
    public async Task RunAsync(Guid sportingEventId, CancellationToken ct) =>
        await mediator.Send(new CollectEventDetailCommand(sportingEventId), ct);
}
