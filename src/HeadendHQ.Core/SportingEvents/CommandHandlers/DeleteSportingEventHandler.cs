using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record DeleteSportingEventCommand(int Id) : ICommand<Unit>;

public class DeleteSportingEventHandler(ISportingEventRepository repository)
    : ICommandHandler<DeleteSportingEventCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSportingEventCommand command, CancellationToken ct)
    {
        var evt = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Sporting event {command.Id} not found.");

        await repository.DeleteAsync(evt, ct);
        return Unit.Value;
    }
}
