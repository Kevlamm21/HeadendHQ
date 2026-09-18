using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Media.CommandHandlers;

public record ClearImagesByPurposeCommand(ImagePurpose Purpose, int? LeagueId = null) : ICommand<int>;

public class ClearImagesByPurposeHandler(
    IWorkspace workspace,
    ILogger<ClearImagesByPurposeHandler> logger)
    : ICommandHandler<ClearImagesByPurposeCommand, int>
{
    public async ValueTask<int> Handle(ClearImagesByPurposeCommand command, CancellationToken ct)
    {
        var images = await workspace.Load(new ImagesByPurposeSpec(command.Purpose, command.LeagueId), ct);

        if (images.Count == 0)
            return 0;

        foreach (var image in images)
            workspace.Remove(image);

        if (command.Purpose == ImagePurpose.Headshot)
            foreach (var sportingEvent in await workspace.Load(
                         new FutureEventsForRecollectSpec(DateTime.UtcNow, command.LeagueId), ct))
                sportingEvent.ResetDetails();

        logger.LogInformation(
            "Cleared {Count} {Purpose} image(s){Scope}.",
            images.Count, command.Purpose,
            command.LeagueId is { } leagueId ? $" for league {leagueId}" : string.Empty);

        return images.Count;
    }
}
