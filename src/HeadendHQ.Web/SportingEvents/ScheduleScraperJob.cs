using HeadendHQ.Core.SportingEvents.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.SportingEvents;

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
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ScrapeSchedulesCommand(), ct);
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
