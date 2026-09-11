using Hangfire;
using HeadendHQ.Core.Catalog.Broadcasters.CommandHandlers;
using HeadendHQ.Core.Catalog.Leagues.CommandHandlers;
using HeadendHQ.Core.Events.CommandHandlers;
using HeadendHQ.Core.Iptv.CommandHandlers;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.VodLauncher.EventHandlers;
using Mediator;

namespace HeadendHQ.Web.Jobs;

public class NightlyJob(IMediator mediator, ILogger<NightlyJob> logger)
{
    public const string RecurringJobId = "nightly";

    public async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("Nightly job starting.");

        try
        {
            await mediator.Send(new RefreshIptvGuideCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "IPTV programme-guide refresh failed.");
        }

        try
        {
            await mediator.Send(new RefreshIptvLineupCommand(), ct);
            var matched = await mediator.Send(new RematchAffiliatesCommand(), ct);
            if (matched > 0)
                logger.LogInformation("Matched {Count} affiliate(s) to a lineup channel.", matched);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "IPTV lineup refresh failed.");
        }

        try
        {
            var parsed = await mediator.Send(new ParseIptvGuideCommand(), ct);
            logger.LogInformation("Parsed {Count} programme-guide entry/entries.", parsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Programme-guide parsing failed.");
        }

        try
        {
            await mediator.Send(new RefreshFollowedLeaguesCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Followed-league refresh failed.");
        }

        // Must run before anything creates titles, or new titles get their production enqueued twice.
        try
        {
            var resumed = await mediator.Send(new ResumeTitleProductionCommand(), ct);
            if (resumed.AdbMappings > 0 || resumed.Productions > 0)
                logger.LogInformation(
                    "Resumed {Adb} ADB mapping(s) and {Production} production job(s) left by earlier runs.",
                    resumed.AdbMappings, resumed.Productions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Resuming outstanding title production failed.");
        }

        try
        {
            var result = await mediator.Send(new ImportScheduleCommand(), ct);
            logger.LogInformation("Schedule import: {Upserted} event(s) upserted, {Removed} removed.",
                result.EventsUpserted, result.EventsRemoved);

            if (result.Errors.Count > 0)
                logger.LogWarning("Schedule import completed with {Count} error(s): {Errors}",
                    result.Errors.Count, result.Errors);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Schedule scrape failed.");
        }

        try
        {
            var resolved = await mediator.Send(new RefreshBroadcasterDetailsCommand(), ct);
            if (resolved > 0)
                logger.LogInformation("Resolved {Count} outstanding broadcaster record(s).", resolved);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Broadcaster detail refresh failed.");
        }

        try
        {
            var created = await mediator.Send(new CreateTitlesForTodayCommand(), ct);
            logger.LogInformation("Created {Count} title(s) for today's events.", created);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Title creation failed.");
        }

        try
        {
            var globalSettings = await mediator.Send(new GetGlobalSettingsQuery(), ct);
            await mediator.Send(new CleanupExpiredCommand(globalSettings.TitleRetentionDays), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Title and event cleanup failed.");
        }

        try
        {
            await mediator.Send(new CleanupExpiredVodCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "VOD cleanup failed.");
        }

        logger.LogInformation("Nightly job complete.");
    }
}
