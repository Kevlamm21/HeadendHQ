using HeadendHQ.Core.Events.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Events.CommandHandlers;

/// <summary>
/// Drops sporting events that finished long enough ago to be of no further use.
/// <para>
/// Events accumulate faster than titles — the whole scrape window arrives every night, while only
/// the day's games become titles — so without this the table would grow forever. Titles have their
/// own retention and are not touched here.
/// </para>
/// </summary>
public record CleanupExpiredEventsCommand(int RetentionDays) : ICommand<int>;

public class CleanupExpiredEventsHandler(IWorkspace workspace, ILogger<CleanupExpiredEventsHandler> logger)
    : ICommandHandler<CleanupExpiredEventsCommand, int>
{
    public async ValueTask<int> Handle(CleanupExpiredEventsCommand command, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-command.RetentionDays);
        var expired = await workspace.Load(new ExpiredEventsSpec(cutoff), ct);

        foreach (var sportingEvent in expired)
            workspace.Remove(sportingEvent);

        if (expired.Count > 0)
            logger.LogInformation("Removed {Count} sporting event(s) older than {Days} day(s).",
                expired.Count, command.RetentionDays);

        return expired.Count;
    }
}
