using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Titles;

public class ScheduleScraperJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduleScraperJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunJobAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            logger.LogInformation("Next schedule scrape in {Hours}h {Minutes}m.",
                (int)delay.TotalHours, delay.Minutes);

            await Task.Delay(delay, stoppingToken);
            await RunJobAsync(stoppingToken);
        }
    }

    private async Task RunJobAsync(CancellationToken ct)
    {
        logger.LogInformation("Schedule scrape job starting.");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new ScrapeSchedulesCommand(), ct);

            if (result.SourceErrors.Count > 0)
                logger.LogWarning(
                    "Schedule scrape completed with {ErrorCount} source error(s): {Errors}",
                    result.SourceErrors.Count, result.SourceErrors);
            else
                logger.LogInformation(
                    "Schedule scrape completed. {Count} events upserted.", result.TotalUpserted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Schedule scrape job failed unexpectedly.");
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var nextRun = DateTime.Today.AddHours(3);

        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }
}
