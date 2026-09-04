using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record DeleteSportingEventCommand(Guid Id) : ICommand<Unit>;

public class DeleteSportingEventHandler(IWorkspace workspace)
    : ICommandHandler<DeleteSportingEventCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSportingEventCommand command, CancellationToken ct)
    {
        var sportingEvent = await workspace.LoadById<SportingEvent, Guid>(command.Id, ct);
        workspace.Remove(sportingEvent);
        return Unit.Value;
    }
}
