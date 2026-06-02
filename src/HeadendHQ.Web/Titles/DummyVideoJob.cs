using HeadendHQ.DummyVideo;
using HeadendHQ.DummyVideo.EventHandlers;
using Mediator;
using Microsoft.Extensions.Options;

namespace HeadendHQ.Web.Titles;

public class DummyVideoJob(
    IServiceScopeFactory scopeFactory,
    IOptions<DummyVideoOptions> options,
    ILogger<DummyVideoJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunJobAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
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

    private TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var nextRun = DateTime.Today.Add(options.Value.RunTime.ToTimeSpan());

        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }
}
