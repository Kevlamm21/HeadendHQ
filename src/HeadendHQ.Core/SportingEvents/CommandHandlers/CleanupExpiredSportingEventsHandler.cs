using Mediator;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record CleanupExpiredSportingEventsCommand(int RetentionDays) : ICommand<Unit>;

public class CleanupExpiredSportingEventsHandler(ISportingEventRepository repository)
    : ICommandHandler<CleanupExpiredSportingEventsCommand, Unit>
{
    public async ValueTask<Unit> Handle(CleanupExpiredSportingEventsCommand command, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-command.RetentionDays);
        await repository.DeleteExpiredAsync(cutoff, ct);
        return Unit.Value;
    }
}
