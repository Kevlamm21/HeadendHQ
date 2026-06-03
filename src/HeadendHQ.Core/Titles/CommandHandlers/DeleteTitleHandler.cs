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
        workspace.Remove(title);
        return Unit.Value;
    }
}
