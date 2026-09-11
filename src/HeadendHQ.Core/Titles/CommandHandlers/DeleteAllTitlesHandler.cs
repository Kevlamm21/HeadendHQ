using HeadendHQ.Core.Events;
using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Titles.CommandHandlers;

/// <summary>
/// Debugging aid: wipes every title, and the event each one was produced from, so production can be
/// replayed from scratch without dropping the database. The catalog survives; a fresh scrape brings
/// the events back.
/// </summary>
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
