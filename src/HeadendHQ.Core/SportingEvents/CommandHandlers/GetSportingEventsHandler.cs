using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record GetSportingEventsQuery(DateTime? From, DateTime? To, Sport? Sport)
    : ICommand<List<SportingEvent>>;

public class GetSportingEventsHandler(ISportingEventRepository repository)
    : ICommandHandler<GetSportingEventsQuery, List<SportingEvent>>
{
    public async ValueTask<List<SportingEvent>> Handle(GetSportingEventsQuery query, CancellationToken ct)
    {
        return await repository.GetAsync(query.From, query.To, query.Sport, ct);
    }
}
