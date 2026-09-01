using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleCreatedHandler(IBackgroundJobClient jobClient, IWorkspace workspace) : INotificationHandler<TitleCreated>
{
    public async ValueTask Handle(TitleCreated notification, CancellationToken cancellationToken)
    {
        if (notification.StartUtc is not { } startUtc)
            return;

        if (startUtc.ToLocalTime().Date != DateTime.Now.Date)
            return;

        var scheduledAt = new DateTimeOffset(startUtc, TimeSpan.Zero);

        if (scheduledAt <= DateTimeOffset.UtcNow)
            return;

        var jobId = jobClient.Schedule<TitleGoesLiveService>(s => s.MarkAsLiveAsync(notification.TitleId, CancellationToken.None), scheduledAt);

        var title = await workspace.LoadById<Title, Guid>(notification.TitleId, cancellationToken);
        title.SetLiveJobId(jobId);
    }
}
