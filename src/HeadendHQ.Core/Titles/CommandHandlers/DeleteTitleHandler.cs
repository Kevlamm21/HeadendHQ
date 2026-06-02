using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record DeleteTitleCommand(Guid Id) : ICommand<Unit>;

public class DeleteTitleHandler(IWorkspace workspace, IUnitOfWork uow)
    : ICommandHandler<DeleteTitleCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteTitleCommand command, CancellationToken ct)
    {
        var results = await workspace.Load(new TitleByIdSpec(command.Id), ct);
        var title = results.FirstOrDefault()
            ?? throw new InvalidOperationException($"Title {command.Id} not found.");

        workspace.Remove(title);
        await uow.SaveChanges(ct);
        return Unit.Value;
    }
}
