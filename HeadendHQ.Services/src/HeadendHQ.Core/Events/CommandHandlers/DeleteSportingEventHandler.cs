using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record DeleteSportingEventCommand(Guid Id) : ICommand<Unit>;

public class DeleteSportingEventHandler(IWorkspace workspace)
    : ICommandHandler<DeleteSportingEventCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSportingEventCommand command, CancellationToken ct)
    {
        var sportingEvent = await workspace.LoadById<SportingEvent, Guid>(command.Id, ct);

        Title[] titles = [];
        if (sportingEvent.TitleId is { } titleId
            && await workspace.LoadSingleOrDefault(new EntityByIdSpecification<Title, Guid>(titleId), ct) is { } title)
            titles = [title];

        await TitleEventCleanup.RemoveAsync(workspace, [sportingEvent], titles, ct);
        return Unit.Value;
    }
}
