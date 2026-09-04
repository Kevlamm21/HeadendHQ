using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

/// <summary>
/// Debugging aid: removes every sporting event so schedule and title production can be replayed
/// from scratch without dropping the database.
/// </summary>
public record DeleteAllSportingEventsCommand : ICommand<int>;

public class DeleteAllSportingEventsHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllSportingEventsCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllSportingEventsCommand command, CancellationToken ct)
    {
        var sportingEvents = await workspace.LoadAll<SportingEvent>(ct);

        foreach (var sportingEvent in sportingEvents)
            workspace.Remove(sportingEvent);

        return sportingEvents.Count;
    }
}
