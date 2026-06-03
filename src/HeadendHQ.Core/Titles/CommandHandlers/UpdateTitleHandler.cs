using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record UpdateTitleCommand(Guid Id, TitleRequest Request) : ICommand<Title>;

public class UpdateTitleHandler(IWorkspace workspace)
    : ICommandHandler<UpdateTitleCommand, Title>
{
    public async ValueTask<Title> Handle(UpdateTitleCommand command, CancellationToken ct)
    {
        var title = await workspace.LoadById<Title, Guid>(command.Id, ct);
        title.Update(command.Request);
        return title;
    }
}
