using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record UpdateTitleCommand(Guid Id, TitleRequest Request) : ICommand<Title>;

public class UpdateTitleHandler(IWorkspace workspace, IUnitOfWork uow)
    : ICommandHandler<UpdateTitleCommand, Title>
{
    public async ValueTask<Title> Handle(UpdateTitleCommand command, CancellationToken ct)
    {
        var results = await workspace.Load(new TitleByIdSpec(command.Id), ct);
        var title = results.FirstOrDefault()
            ?? throw new InvalidOperationException($"Title {command.Id} not found.");

        title.Update(command.Request);
        await uow.SaveChanges(ct);
        return title;
    }
}
