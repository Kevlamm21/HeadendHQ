using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record GetSportingEventsQuery(DateTime? FromUtc, DateTime? ToUtc, bool? AwaitingTitle)
    : IQuery<IReadOnlyList<SportingEvent>>;

public class GetSportingEventsHandler(IReadModel readModel)
    : IQueryHandler<GetSportingEventsQuery, IReadOnlyList<SportingEvent>>
{
    public async ValueTask<IReadOnlyList<SportingEvent>> Handle(
        GetSportingEventsQuery query, CancellationToken ct)
    {
        var events = await readModel.Search(
            new SportingEventsInRangeSpec(query.FromUtc, query.ToUtc), ct);

        return query.AwaitingTitle == true ? [.. events.Where(e => e.NeedsTitle)] : events;
    }
}

public record GetSportingEventByIdQuery(Guid Id) : IQuery<SportingEvent?>;

public class GetSportingEventByIdHandler(IReadModel readModel)
    : IQueryHandler<GetSportingEventByIdQuery, SportingEvent?>
{
    public async ValueTask<SportingEvent?> Handle(GetSportingEventByIdQuery query, CancellationToken ct) =>
        await readModel.SingleOrDefault(new EntityByIdSpecification<SportingEvent, Guid>(query.Id), ct);
}

public record DeleteSportingEventCommand(Guid Id) : ICommand<Unit>;

public class DeleteSportingEventHandler(IWorkspace workspace)
    : ICommandHandler<DeleteSportingEventCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSportingEventCommand command, CancellationToken ct)
    {
        var sportingEvent = await workspace.LoadById<SportingEvent, Guid>(command.Id, ct);
        workspace.Remove(sportingEvent);
        return Unit.Value;
    }
}

/// <summary>
/// Debugging aid: removes every sporting event so schedule and title production can be replayed
/// from scratch without dropping the database.
/// </summary>
public record DeleteAllSportingEventsCommand : ICommand<int>;

public class DeleteAllSportingEventsHandler(IWorkspace workspace)
    : ICommandHandler<DeleteAllSportingEventsCommand, int>
{
    public async ValueTask<int> Handle(DeleteAllSportingEventsCommand command, CancellationToken ct)
    {
        var sportingEvents = await workspace.LoadAll<SportingEvent>(ct);

        foreach (var sportingEvent in sportingEvents)
            workspace.Remove(sportingEvent);

        return sportingEvents.Count;
    }
}
