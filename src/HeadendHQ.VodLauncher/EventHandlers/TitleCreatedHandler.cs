using Hangfire;
using HeadendHQ.Core;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.VodLauncher.EventHandlers;

public class TitleCreatedHandler(IBackgroundJobClient jobClient) : INotificationHandler<TitleCreated>
{
    public ValueTask Handle(TitleCreated notification, CancellationToken cancellationToken)
    {
        var jobId = jobClient.Enqueue<AdbMappingService>(s => s.MapSingleAsync(notification.TitleId, CancellationToken.None));
        jobClient.ContinueJobWith<ICreationService>(jobId, s => s.CreateForTitleAsync(notification.TitleId, CancellationToken.None));
        return ValueTask.CompletedTask;
    }
}
