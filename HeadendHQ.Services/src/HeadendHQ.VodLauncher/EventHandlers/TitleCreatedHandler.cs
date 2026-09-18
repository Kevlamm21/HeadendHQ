using Hangfire;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.VodLauncher.EventHandlers;

public class TitleCreatedHandler(IBackgroundJobClient jobClient) : INotificationHandler<TitleCreated>
{
    public ValueTask Handle(TitleCreated notification, CancellationToken cancellationToken)
    {
        TitleProductionJobs.EnqueueAdbMapping(jobClient, notification.TitleId);
        TitleProductionJobs.EnqueueProduction(jobClient, notification.TitleId);
        return ValueTask.CompletedTask;
    }
}
