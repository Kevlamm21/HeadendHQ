using HeadendHQ.Core;

namespace HeadendHQ.Web.HdHomerun;

public class HdHomerunXmltvJob(
    IServiceScopeFactory scopeFactory,
    ILogger<HdHomerunXmltvJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Populate on startup so data is immediately available
        await RunJobAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            logger.LogInformation("Next HDHomeRun XMLTV refresh scheduled in {Hours}h {Minutes}m",
                (int)delay.TotalHours, delay.Minutes);

            await Task.Delay(delay, stoppingToken);
            await RunJobAsync(stoppingToken);
        }
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IHdHomerunService>();

        try
        {
            await service.RefreshXmltvAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to refresh HDHomeRun XMLTV content");
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var nextRun = DateTime.Today.AddHours(2).AddMinutes(30);

        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }
}
