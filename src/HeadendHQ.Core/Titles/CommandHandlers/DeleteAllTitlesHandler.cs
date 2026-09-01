using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

/// <summary>
/// Debugging aid: wipes every title so production can be replayed from scratch without dropping the
/// database. The catalog, the scraped events and the image store all survive; the events simply
/// become eligible for a title again.
/// </summary>
public record DeleteAllTitlesCommand : ICommand<int>;

public class DeleteAllTitlesHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllTitlesCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllTitlesCommand command, CancellationToken ct)
    {
        var titles = await workspace.LoadAll<Title>(ct);

        foreach (var title in titles)
        {
            title.MarkDeleted();
            workspace.Remove(title);
        }

        return titles.Count;
    }
}
