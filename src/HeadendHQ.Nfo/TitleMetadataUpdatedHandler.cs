using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleMetadataUpdatedHandler(IBackgroundJobClient jobClient, IWorkspace workspace) : INotificationHandler<TitleMetadataUpdated>
{
    public async ValueTask Handle(TitleMetadataUpdated notification, CancellationToken cancellationToken)
    {
        var title = await workspace.LoadById<Title, Guid>(notification.TitleId, cancellationToken);

        if (title.VodLauncherPath is not null && title.Production.WritesNfo)
            jobClient.Enqueue<NfoWriter>(w => w.WriteForTitleAsync(notification.TitleId, CancellationToken.None));

        // An unproduced title gets its go-live scheduled by production.
        if (notification.ScheduleChanged && title.IsVideoCreated)
            GoLiveScheduler.Reschedule(jobClient, title);
    }
}
