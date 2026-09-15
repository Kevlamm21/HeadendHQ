using HeadendHQ.Core.Streaming.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Jobs;

public class StreamingServiceSyncJob(IMediator mediator, ILogger<StreamingServiceSyncJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var added = await mediator.Send(new SyncStreamingServicesCommand(), ct);
        logger.LogInformation("Streaming service sync added {Count} service(s).", added);
    }
}
