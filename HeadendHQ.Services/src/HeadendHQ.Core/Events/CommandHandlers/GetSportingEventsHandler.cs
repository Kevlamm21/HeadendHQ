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
