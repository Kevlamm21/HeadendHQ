using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleMetadataUpdatedHandler(IBackgroundJobClient jobClient, IWorkspace workspace) : INotificationHandler<TitleMetadataUpdated>
{
    public async ValueTask Handle(TitleMetadataUpdated notification, CancellationToken cancellationToken)
    {
        jobClient.Enqueue<NfoWriter>(w => w.WriteForTitleAsync(notification.TitleId, CancellationToken.None));

        var title = await workspace.LoadById<Title, Guid>(notification.TitleId, cancellationToken);

        if (title.LiveJobId is not null)
        {
            jobClient.Delete(title.LiveJobId);
            title.LiveJobId = null;
        }

        if (title.StartUtc is not { } startUtc)
            return;

        if (startUtc.ToLocalTime().Date != DateTime.Now.Date)
            return;

        var scheduledAt = new DateTimeOffset(startUtc, TimeSpan.Zero);

        if (scheduledAt <= DateTimeOffset.UtcNow)
            return;

        title.LiveJobId = jobClient.Schedule<TitleGoesLiveService>(s => s.MarkAsLiveAsync(notification.TitleId, CancellationToken.None), scheduledAt);
    }
}
