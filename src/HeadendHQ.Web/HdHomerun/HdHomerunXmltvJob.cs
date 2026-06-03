using Cronos;
using HeadendHQ.HdHomerun;
using HeadendHQ.HdHomerun.CommandHandlers;
using HeadendHQ.HdHomerun.Settings;
using Mediator;

namespace HeadendHQ.Web.CronJobs;

public class HdHomerunXmltvJob(
    IServiceScopeFactory scopeFactory,
    ILogger<HdHomerunXmltvJob> logger) : BackgroundService
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
            logger.LogInformation("Next HDHomeRun XMLTV refresh in {Hours}h {Minutes}m.",
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
            await mediator.Send(new RefreshXmltvCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to refresh HDHomeRun XMLTV content.");
        }
    }

    private async Task<HdHomerunSettings> GetSettingsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetHdHomerunSettingsQuery(), ct);
    }

    private static TimeSpan GetDelay(string cronSchedule)
    {
        var expression = CronExpression.Parse(cronSchedule);
        var next = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        return next.HasValue ? next.Value - DateTimeOffset.Now : TimeSpan.FromHours(24);
    }
}
