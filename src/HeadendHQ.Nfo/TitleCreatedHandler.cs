using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleCreatedHandler(IBackgroundJobClient jobClient, IWorkspace workspace) : INotificationHandler<TitleCreated>
{
    public async ValueTask Handle(TitleCreated notification, CancellationToken cancellationToken)
    {
        var title = await workspace.LoadById<Title, Guid>(notification.TitleId, cancellationToken);
        GoLiveScheduler.Reschedule(jobClient, title);
    }
}
