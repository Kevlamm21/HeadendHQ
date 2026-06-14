using Hangfire;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleCreatedHandler(IBackgroundJobClient jobClient) : INotificationHandler<TitleCreated>
{
    public ValueTask Handle(TitleCreated notification, CancellationToken cancellationToken)
    {
        if (notification.StartUtc is not { } startUtc)
            return ValueTask.CompletedTask;

        if (startUtc.ToLocalTime().Date != DateTime.Now.Date)
            return ValueTask.CompletedTask;

        var scheduledAt = new DateTimeOffset(startUtc, TimeSpan.Zero);

        if (scheduledAt > DateTimeOffset.UtcNow)
            jobClient.Schedule<TitleGoesLiveService>(s => s.MarkAsLiveAsync(notification.TitleId, CancellationToken.None), scheduledAt);

        return ValueTask.CompletedTask;
    }
}
