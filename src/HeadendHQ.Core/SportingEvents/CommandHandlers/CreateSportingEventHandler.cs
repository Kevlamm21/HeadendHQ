using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record CreateSportingEventCommand(SportingEventRequest Request) : ICommand<SportingEvent>;

public class CreateSportingEventHandler(ISportingEventRepository repository)
    : ICommandHandler<CreateSportingEventCommand, SportingEvent>
{
    public async ValueTask<SportingEvent> Handle(CreateSportingEventCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var existing = await repository.FindByProviderTitleStartAsync(
            request.Provider, request.Title, request.StartUtc, ct);

        if (existing is not null)
        {
            existing.Update(request);
            await repository.SaveAsync(ct);
            return existing;
        }

        var evt = new SportingEvent(request);
        await repository.AddAsync(evt, ct);
        return evt;
    }
}
