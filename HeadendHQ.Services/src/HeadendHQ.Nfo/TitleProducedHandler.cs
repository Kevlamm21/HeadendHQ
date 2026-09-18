using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleProducedHandler(IBackgroundJobClient jobClient, IWorkspace workspace) : INotificationHandler<TitleProduced>
{
    public async ValueTask Handle(TitleProduced notification, CancellationToken cancellationToken)
    {
        var title = await workspace.LoadById<Title, Guid>(notification.TitleId, cancellationToken);
        GoLiveScheduler.Reschedule(jobClient, title);
    }
}
