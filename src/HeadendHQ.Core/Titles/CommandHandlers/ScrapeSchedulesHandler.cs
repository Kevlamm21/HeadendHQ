using HeadendHQ.Core.Options;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record ScrapeSchedulesCommand : ICommand<ScrapeSchedulesResult>;

public record ScrapeSchedulesResult(int TotalUpserted, List<string> SourceErrors);

public class ScrapeSchedulesHandler(
    IEnumerable<IScheduleSource> sources,
    IMediator mediator,
    IOptions<ScheduleScraperOptions> options,
    ILogger<ScrapeSchedulesHandler> logger)
    : ICommandHandler<ScrapeSchedulesCommand, ScrapeSchedulesResult>
{
    public async ValueTask<ScrapeSchedulesResult> Handle(ScrapeSchedulesCommand command, CancellationToken ct)
    {
        var totalUpserted = 0;
        var sourceErrors = new List<string>();

        foreach (var source in sources)
        {
            try
            {
                totalUpserted += await source.FetchEventsAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to scrape events from {Source}.", source);
                sourceErrors.Add($"{source}: {ex.Message}");
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
            await mediator.Send(new CleanupExpiredTitlesCommand(options.Value.CleanupRetentionDays), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Cleanup of expired titles failed.");
        }

        return new ScrapeSchedulesResult(totalUpserted, sourceErrors);
    }
}
