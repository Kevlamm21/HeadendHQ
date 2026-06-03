using Cronos;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.CronJobs;

public class ScheduleScraperJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduleScraperJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = await GetSettingsAsync(stoppingToken);
        if (settings.RunOnStartup)
            await RunJobAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            settings = await GetSettingsAsync(stoppingToken);
            var delay = GetDelay(settings.CronSchedule);
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

    private async Task<ScheduleScraperSettings> GetSettingsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetScheduleScraperSettingsQuery(), ct);
    }

    private static TimeSpan GetDelay(string cronSchedule)
    {
        var expression = CronExpression.Parse(cronSchedule);
        var next = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        return next.HasValue ? next.Value - DateTimeOffset.Now : TimeSpan.FromHours(24);
    }
}
