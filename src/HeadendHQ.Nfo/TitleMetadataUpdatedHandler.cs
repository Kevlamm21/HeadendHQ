using Hangfire;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Nfo;

public class TitleMetadataUpdatedHandler(IBackgroundJobClient jobClient) : INotificationHandler<TitleMetadataUpdated>
{
    public ValueTask Handle(TitleMetadataUpdated notification, CancellationToken cancellationToken)
    {
        jobClient.Enqueue<NfoWriter>(w => w.WriteForTitleAsync(notification.TitleId, CancellationToken.None));
        return ValueTask.CompletedTask;
    }
}
