using HeadendHQ.Core.Events;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record DeleteTitleCommand(Guid Id) : ICommand<Unit>;

public class DeleteTitleHandler(IWorkspace workspace)
    : ICommandHandler<DeleteTitleCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteTitleCommand command, CancellationToken ct)
    {
        var title = await workspace.LoadById<Title, Guid>(command.Id, ct);
        var events = await workspace.Load(new EventsByTitleIdsSpec([title.Id]), ct);

        await TitleEventCleanup.RemoveAsync(workspace, events, [title], ct);
        return Unit.Value;
    }
}
