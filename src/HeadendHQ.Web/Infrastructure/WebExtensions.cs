using Hangfire;
using HeadendHQ.Core.Catalog.CommandHandlers;
using HeadendHQ.Web.Jobs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HeadendHQ.Web.Infrastructure;

public static class WebExtensions
{
    private const string DefaultNightlySchedule = "0 6 * * *";

    public static void ConfigureWeb(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<NightlyJob>();
        builder.Services.AddScoped<CatalogSeedJob>();
        builder.Services.AddScoped<BroadcasterDiscoveryJob>();
    }

    /// <summary>
    /// Registers the scheduled work with Hangfire.
    /// <para>
    /// Storage is in-memory, so this runs on every boot by design — <c>AddOrUpdate</c> is keyed by
    /// job id and simply overwrites, which also means a changed cron expression takes effect on
    /// restart rather than leaving a stale schedule behind. The seed is enqueued unconditionally and
    /// decides for itself whether there is anything to do.
    /// </para>
    /// </summary>
    public static void UseJobs(this WebApplication app)
    {
        var schedule = app.Configuration["NightlyJob:CronSchedule"] ?? DefaultNightlySchedule;

        RecurringJob.AddOrUpdate<NightlyJob>(
            NightlyJob.RecurringJobId,
            job => job.RunAsync(CancellationToken.None),
            schedule,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

        if (bool.TryParse(app.Configuration["NightlyJob:RunOnStartup"], out var runOnStartup) && runOnStartup)
            BackgroundJob.Enqueue<NightlyJob>(job => job.RunAsync(CancellationToken.None));

        BackgroundJob.Enqueue<CatalogSeedJob>(job => job.RunAsync(CancellationToken.None));

        // The broadcaster crawl owes a run whenever it has never completed — a fresh database, or a
        // crawl that was interrupted (both leave the marker unset). A completed crawl is re-enqueued
        // nightly by NightlyJob and costs only the two index requests.
        using (var scope = app.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var state = mediator.Send(new GetCatalogSyncStateQuery()).AsTask().GetAwaiter().GetResult();
            if (state?.LastBroadcasterRefreshUtc is null)
                BackgroundJob.Enqueue<BroadcasterDiscoveryJob>(job => job.RunAsync(CancellationToken.None));
        }
    }
}
