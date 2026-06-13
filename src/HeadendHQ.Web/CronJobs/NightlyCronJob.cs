using Cronos;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.ScheduleScraping;
using HeadendHQ.VodLauncher.EventHandlers;
using HeadendHQ.HdHomerun.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.CronJobs;

public class NightlyCronJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<NightlyCronJob> logger) : BackgroundService
{
    private string CronSchedule => configuration["NightlyJob:CronSchedule"] ?? "0 6 * * *";
    private bool RunOnStartup => bool.TryParse(configuration["NightlyJob:RunOnStartup"], out var v) && v;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (RunOnStartup)
            await RunJobAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelay(CronSchedule);
            logger.LogInformation("Next nightly job in {Hours}h {Minutes}m.", (int)delay.TotalHours, delay.Minutes);
            await Task.Delay(delay, stoppingToken);
            await RunJobAsync(stoppingToken);
        }
    }

    private async Task RunJobAsync(CancellationToken ct)
    {
        logger.LogInformation("Nightly job starting.");
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new RefreshXmltvCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "HDHomeRun XMLTV refresh failed.");
        }

        try
        {
            var result = await mediator.Send(new ScrapeSchedulesCommand(), ct);
            if (result.SourceErrors.Count > 0)
                logger.LogWarning("Schedule scrape completed with {Count} error(s): {Errors}",
                    result.SourceErrors.Count, result.SourceErrors);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Schedule scrape failed.");
        }

        try
        {
            var globalSettings = await mediator.Send(new GetGlobalSettingsQuery(), ct);
            await mediator.Send(new CleanupExpiredTitlesCommand(globalSettings.TitleRetentionDays), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Title cleanup failed.");
        }

        try
        {
            await mediator.Send(new CleanupExpiredVodCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "VOD cleanup failed.");
        }

        try
        {
            var count = await mediator.Send(new CreateVodLaunchersCommand(), ct);
            logger.LogInformation("Enqueued {Count} production jobs for today.", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Production job enqueueing failed.");
        }

        logger.LogInformation("Nightly job complete.");
    }

    private static TimeSpan GetDelay(string cronSchedule)
    {
        var expression = CronExpression.Parse(cronSchedule);
        var next = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        return next.HasValue ? next.Value - DateTimeOffset.Now : TimeSpan.FromHours(24);
    }
}
