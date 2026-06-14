using HeadendHQ.Core;
using HeadendHQ.ScheduleScraping.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.ScheduleScraping;

public record ScrapeSchedulesCommand : ICommand<ScrapeSchedulesResult>;

public record ScrapeSchedulesResult(int TotalUpserted, List<string> SourceErrors);

public class ScrapeSchedulesHandler(
    IEnumerable<IScheduleScraper> sources,
    IMediator mediator,
    ILogger<ScrapeSchedulesHandler> logger)
    : ICommandHandler<ScrapeSchedulesCommand, ScrapeSchedulesResult>
{
    public async ValueTask<ScrapeSchedulesResult> Handle(ScrapeSchedulesCommand command, CancellationToken ct)
    {
        var settings = await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct);
        var totalUpserted = 0;
        var sourceErrors = new List<string>();

        foreach (var source in sources)
        {
            try
            {
                totalUpserted += await source.FetchEventsAsync(settings.ScrapeWindowDays, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to scrape events from {Source}.", source);
                sourceErrors.Add($"{source}: {ex.Message}");
            }
        }

        return new ScrapeSchedulesResult(totalUpserted, sourceErrors);
    }
}
