using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record UpdateSportingEventCommand(int Id, SportingEventRequest Request) : ICommand<SportingEvent>;

public class UpdateSportingEventHandler(ISportingEventRepository repository)
    : ICommandHandler<UpdateSportingEventCommand, SportingEvent>
{
    public async ValueTask<SportingEvent> Handle(UpdateSportingEventCommand command, CancellationToken ct)
    {
        var evt = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Sporting event {command.Id} not found.");

        evt.Update(command.Request);
        await repository.SaveAsync(ct);
        return evt;
    }
}
