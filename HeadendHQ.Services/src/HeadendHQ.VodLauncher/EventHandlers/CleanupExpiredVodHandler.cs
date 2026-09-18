using HeadendHQ.Core;
using Mediator;

namespace HeadendHQ.VodLauncher.EventHandlers;

public record CleanupExpiredVodCommand : ICommand<Unit>;

public class CleanupExpiredVodHandler(ICleanupService cleanupService)
    : ICommandHandler<CleanupExpiredVodCommand, Unit>
{
    public async ValueTask<Unit> Handle(CleanupExpiredVodCommand command, CancellationToken ct)
    {
        await cleanupService.CleanupExpiredAsync(ct);
        return Unit.Value;
    }
}
