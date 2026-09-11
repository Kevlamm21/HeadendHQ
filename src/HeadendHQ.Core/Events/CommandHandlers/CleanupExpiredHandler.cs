using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

/// <summary>
/// Drops sporting events and titles that finished long enough ago to be of no further use, plus any
/// artwork/headshot images that no longer have a reference anywhere.
/// <para>
/// A sports title and the event it was produced from are one lifecycle — a title's <c>EndUtc</c> is
/// copied from its event's, so an expired event's title is always independently expired too. Both
/// specs are queried and removed together in one pass so their shared images can be swept correctly;
/// a title with no backing event (e.g. a future non-sports title type) still expires on its own.
/// </para>
/// </summary>
public record CleanupExpiredCommand(int RetentionDays) : ICommand<Unit>;

public class CleanupExpiredHandler(IWorkspace workspace, ILogger<CleanupExpiredHandler> logger)
    : ICommandHandler<CleanupExpiredCommand, Unit>
{
    public async ValueTask<Unit> Handle(CleanupExpiredCommand command, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-command.RetentionDays);
        var expiredEvents = await workspace.Load(new ExpiredEventsSpec(cutoff), ct);
        var expiredTitles = await workspace.Load(new ExpiredTitlesSpec(cutoff), ct);

        var imagesRemoved = await TitleEventCleanup.RemoveAsync(workspace, expiredEvents, expiredTitles, ct);

        if (expiredEvents.Count > 0 || expiredTitles.Count > 0)
            logger.LogInformation(
                "Removed {Events} expired event(s), {Titles} expired title(s), {Images} orphaned image(s).",
                expiredEvents.Count, expiredTitles.Count, imagesRemoved);

        return Unit.Value;
    }
}
