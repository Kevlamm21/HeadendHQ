using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record GetSportingEventByIdQuery(int Id) : ICommand<SportingEvent?>;

public class GetSportingEventByIdHandler(ISportingEventRepository repository)
    : ICommandHandler<GetSportingEventByIdQuery, SportingEvent?>
{
    public async ValueTask<SportingEvent?> Handle(GetSportingEventByIdQuery query, CancellationToken ct)
    {
        return await repository.GetByIdAsync(query.Id, ct);
    }
}
