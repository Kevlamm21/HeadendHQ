using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeadendHQ.Core.SportingEvents.CommandHandlers;

public record ScrapeSchedulesCommand : ICommand<Unit>;

public class ScrapeSchedulesHandler(
    IEnumerable<IScheduleSource> sources,
    IMediator mediator,
    IOptions<ScheduleScraperOptions> options,
    ILogger<ScrapeSchedulesHandler> logger)
    : ICommandHandler<ScrapeSchedulesCommand, Unit>
{
    public async ValueTask<Unit> Handle(ScrapeSchedulesCommand command, CancellationToken ct)
    {
        foreach (var source in sources)
        {
            try
            {
                await source.FetchEventsAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to scrape events from {Sport}.", source.Sport);
            }
        }

        try
        {
            await mediator.Send(new MapAdbCommandsCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "ADB mapping failed.");
        }

        try
        {
            await mediator.Send(new CleanupExpiredSportingEventsCommand(options.Value.CleanupRetentionDays), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Cleanup of expired sporting events failed.");
        }

        return Unit.Value;
    }
}
