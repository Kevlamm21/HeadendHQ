using Hangfire;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;

namespace HeadendHQ.VodLauncher.EventHandlers;

public record MapPendingAdbCommand : ICommand<int>;

/// <summary>
/// Hangfire uses volatile in-memory storage, so mapping jobs in flight are lost on
/// restart with nothing to recover them. This re-enqueues upcoming titles that still
/// have no ADB command.
/// </summary>
public class MapPendingAdbHandler(
    IReadModel readModel,
    IBackgroundJobClient jobClient) : ICommandHandler<MapPendingAdbCommand, int>
{
    public async ValueTask<int> Handle(MapPendingAdbCommand command, CancellationToken ct)
    {
        var titles = await readModel.Search(new PendingAdbMappingSpec(DateTime.UtcNow), ct);

        foreach (var title in titles)
            jobClient.Enqueue<AdbMappingService>(s => s.MapSingleAsync(title.Id, CancellationToken.None));

        return titles.Count;
    }
}
