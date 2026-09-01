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

        // Future titles have no VOD folder yet. Their NFO is written by VodCreationService
        // once the folder exists on game day, so enqueueing a write here would only fail.
        if (title.VodLauncherPath is not null)
            jobClient.Enqueue<NfoWriter>(w => w.WriteForTitleAsync(notification.TitleId, CancellationToken.None));

        if (title.LiveJobId is not null)
        {
            jobClient.Delete(title.LiveJobId);
            title.SetLiveJobId(null);
        }

        if (title.StartUtc is not { } startUtc)
            return;

        if (startUtc.ToLocalTime().Date != DateTime.Now.Date)
            return;

        var scheduledAt = new DateTimeOffset(startUtc, TimeSpan.Zero);

        if (scheduledAt <= DateTimeOffset.UtcNow)
            return;

        title.SetLiveJobId(jobClient.Schedule<TitleGoesLiveService>(s => s.MarkAsLiveAsync(notification.TitleId, CancellationToken.None), scheduledAt));
    }
}
