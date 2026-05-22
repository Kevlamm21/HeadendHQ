using HeadendHQ.Core.SportingEvents;
using Mediator;

namespace HeadendHQ.DummyVideo.EventHandlers;

public record CreateDummyVideosCommand : ICommand<Unit>;

public class CreateDummyVideosHandler(IVideoCreationService videoCreationService)
    : ICommandHandler<CreateDummyVideosCommand, Unit>
{
    public async ValueTask<Unit> Handle(CreateDummyVideosCommand command, CancellationToken ct)
    {
        await videoCreationService.CreateDailyAsync(ct);
        return Unit.Value;
    }
}
