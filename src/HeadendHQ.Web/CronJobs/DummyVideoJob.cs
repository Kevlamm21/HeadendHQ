using Cronos;
using HeadendHQ.DummyVideo;
using HeadendHQ.DummyVideo.EventHandlers;
using HeadendHQ.DummyVideo.Settings;
using Mediator;

namespace HeadendHQ.Web.CronJobs;

public class DummyVideoJob(
    IServiceScopeFactory scopeFactory,
    ILogger<DummyVideoJob> logger) : BackgroundService
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
            logger.LogInformation("Next video job in {Hours}h {Minutes}m.",
                (int)delay.TotalHours, delay.Minutes);

            await Task.Delay(delay, stoppingToken);
            await RunJobAsync(stoppingToken);
        }
    }

    private async Task RunJobAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new CleanupExpiredVideosCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Video cleanup failed.");
        }

        try
        {
            await mediator.Send(new CreateDummyVideosCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Video creation failed.");
        }
    }

    private async Task<DummyVideoSettings> GetSettingsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetDummyVideoSettingsQuery(), ct);
    }

    private static TimeSpan GetDelay(string cronSchedule)
    {
        var expression = CronExpression.Parse(cronSchedule);
        var next = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        return next.HasValue ? next.Value - DateTimeOffset.Now : TimeSpan.FromHours(24);
    }
}
