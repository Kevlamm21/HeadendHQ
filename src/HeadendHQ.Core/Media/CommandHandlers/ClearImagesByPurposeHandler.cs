using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Media.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Media.CommandHandlers;

/// <summary>
/// Throws away every image of one kind so it is fetched fresh.
/// <para>
/// Exists for media day: leagues reshoot player headshots each preseason at the same URLs, and the
/// URL check that makes a face cost one request ever is exactly what stops the new photo from ever
/// being noticed. Deleting the rows is how you tell the app to look again.
/// </para>
/// <para>
/// Cast rows pointing at a deleted image are left to rot into a 404. Only games within the scrape
/// window exist at all, and they are cleared once played, so the blast radius is the handful of
/// events already collected today — all of which re-collect below and are correct by tomorrow.
/// </para>
/// </summary>
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

        // Detail is collected once per event, ever, so a game already scraped would keep its now
        // headshot-less cast forever. Clearing the stamp is what makes "wipe it and let the next
        // scrape refill it" actually true.
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
