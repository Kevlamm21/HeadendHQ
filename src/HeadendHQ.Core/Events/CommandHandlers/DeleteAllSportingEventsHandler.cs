using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record DeleteAllSportingEventsCommand : ICommand<int>;

public class DeleteAllSportingEventsHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllSportingEventsCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllSportingEventsCommand command, CancellationToken ct)
    {
        var sportingEvents = await workspace.LoadAll<SportingEvent>(ct);
        var titleIds = sportingEvents
            .Where(e => e.TitleId is not null)
            .Select(e => e.TitleId!.Value)
            .ToHashSet();

        var titles = titleIds.Count == 0
            ? []
            : (await workspace.LoadAll<Title>(ct)).Where(t => titleIds.Contains(t.Id)).ToArray();

        await TitleEventCleanup.RemoveAsync(workspace, sportingEvents, titles, ct);
        return sportingEvents.Count;
    }
}
