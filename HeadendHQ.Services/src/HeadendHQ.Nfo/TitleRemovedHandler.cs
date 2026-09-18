using Hangfire;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleRemovedHandler(IBackgroundJobClient jobClient) : INotificationHandler<TitleRemoved>
{
    public ValueTask Handle(TitleRemoved notification, CancellationToken cancellationToken)
    {
        if (notification.LiveJobId is not null)
            jobClient.Delete(notification.LiveJobId);

        return ValueTask.CompletedTask;
    }
}
