using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Events.CommandHandlers;

public record GetSportingEventByIdQuery(Guid Id) : IQuery<SportingEvent?>;

public class GetSportingEventByIdHandler(IReadModel readModel)
    : IQueryHandler<GetSportingEventByIdQuery, SportingEvent?>
{
    public async ValueTask<SportingEvent?> Handle(GetSportingEventByIdQuery query, CancellationToken ct) =>
        await readModel.SingleOrDefault(new EntityByIdSpecification<SportingEvent, Guid>(query.Id), ct);
}
