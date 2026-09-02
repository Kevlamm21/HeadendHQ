using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Core.Events.CommandHandlers;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.VodLauncher.EventHandlers;
using HeadendHQ.Core.Iptv.CommandHandlers;
using Hangfire;
using Mediator;

namespace HeadendHQ.Web.Jobs;

/// <summary>
/// The nightly pass: refresh what the schedule depends on, import it, produce today's titles, then
/// clean up behind yesterday's.
/// <para>
/// A Hangfire recurring job rather than a hosted service, so it shares one scheduler and one
/// dashboard with the per-event detail jobs it enqueues — and so a run can be triggered by hand
/// without restarting the application. Every step is isolated: one upstream failing must not stop
/// the rest of the night's work.
/// </para>
/// </summary>
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

        // Before the scrape's affiliate matching: the lineup is the authority on which local
        // stations this house can tune. Rematch after it, in case discovery ran before it existed.
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

        // After the lineup, not before: a guide entry is only kept if its channel resolves to
        // something tunable, and the lineup is what says which those are.
        try
        {
            var parsed = await mediator.Send(new ParseIptvGuideCommand(), ct);
            logger.LogInformation("Parsed {Count} programme-guide entry/entries.", parsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Programme-guide parsing failed.");
        }

        // Ahead of the scrape: events are created against team colours and logo URLs, so those
        // should be current before any event references them.
        try
        {
            await mediator.Send(new RefreshFollowedLeaguesCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Followed-league refresh failed.");
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

        // After the scrape, because that is what discovers broadcasters in the first place. Most
        // resolve on first sighting; this catches the ones whose lookup failed that night.
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

        // A completed crawl is two index requests; otherwise it resumes where it stopped and also
        // picks up any networks ESPN has added. Its own job so it gets its own request budget.
        try
        {
            BackgroundJob.Enqueue<BroadcasterDiscoveryJob>(job => job.RunAsync(CancellationToken.None));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to enqueue broadcaster discovery.");
        }

        // Detail collection is queued per event by the scrape. Completed detail jobs produce
        // today's titles themselves; this sweep catches anything that was already ready at startup.
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
            await mediator.Send(new CleanupExpiredTitlesCommand(globalSettings.TitleRetentionDays), ct);
            await mediator.Send(new CleanupExpiredEventsCommand(globalSettings.TitleRetentionDays), ct);
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

        try
        {
            var count = await mediator.Send(new MapPendingAdbCommand(), ct);
            if (count > 0)
                logger.LogInformation("Re-enqueued {Count} title(s) still awaiting ADB mapping.", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Pending ADB mapping sweep failed.");
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
}
