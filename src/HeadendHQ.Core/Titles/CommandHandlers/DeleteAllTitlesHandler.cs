using HeadendHQ.Core.Events;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record DeleteAllTitlesCommand : ICommand<int>;

public class DeleteAllTitlesHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllTitlesCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllTitlesCommand command, CancellationToken ct)
    {
        var titles = await workspace.LoadAll<Title>(ct);
        var events = await workspace.Load(new EventsByTitleIdsSpec(titles.Select(t => t.Id).ToArray()), ct);

        await TitleEventCleanup.RemoveAsync(workspace, events, titles, ct);
        return titles.Count;
    }
}
