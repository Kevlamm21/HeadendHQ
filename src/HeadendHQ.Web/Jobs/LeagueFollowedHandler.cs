using Hangfire;
using HeadendHQ.Core.Catalog.Leagues;
using Mediator;

namespace HeadendHQ.Web.Jobs;

public class LeagueFollowedHandler(IBackgroundJobClient jobClient) : INotificationHandler<LeagueFollowed>
{
    public ValueTask Handle(LeagueFollowed notification, CancellationToken cancellationToken)
    {
        jobClient.Enqueue<TeamLogoJob>(job => job.RunAsync(notification.LeagueId, false, CancellationToken.None));
        return ValueTask.CompletedTask;
    }
}
