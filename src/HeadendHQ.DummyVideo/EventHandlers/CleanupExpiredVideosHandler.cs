using HeadendHQ.Core.SportingEvents;
using Mediator;

namespace HeadendHQ.DummyVideo.EventHandlers;

public record CleanupExpiredVideosCommand : ICommand<Unit>;

public class CleanupExpiredVideosHandler(IVideoCleanupService videoCleanupService)
    : ICommandHandler<CleanupExpiredVideosCommand, Unit>
{
    public async ValueTask<Unit> Handle(CleanupExpiredVideosCommand command, CancellationToken ct)
    {
        await videoCleanupService.CleanupExpiredAsync(ct);
        return Unit.Value;
    }
}
