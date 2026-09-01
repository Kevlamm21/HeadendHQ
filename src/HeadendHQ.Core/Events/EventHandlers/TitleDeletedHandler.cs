using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.EventHandlers;

/// <summary>
/// Releases a sporting event when the title made from it is deleted.
/// <para>
/// Without this an expired or manually removed title would leave its event pointing at nothing, and
/// the event could never be produced again — the daily job only looks for events with no title. This
/// lives on the events side so <see cref="Title"/> keeps knowing nothing about sport.
/// </para>
/// </summary>
public class TitleDeletedHandler(IWorkspace workspace, ILogger<TitleDeletedHandler> logger)
    : INotificationHandler<TitleDeleted>
{
    public async ValueTask Handle(TitleDeleted notification, CancellationToken ct)
    {
        var events = await workspace.Load(new EventsByTitleIdsSpec([notification.TitleId]), ct);

        foreach (var sportingEvent in events)
        {
            sportingEvent.DetachTitle();
            logger.LogInformation(
                "Released {Away} at {Home} after its title was deleted.",
                sportingEvent.AwayTeamName, sportingEvent.HomeTeamName);
        }
    }
}
