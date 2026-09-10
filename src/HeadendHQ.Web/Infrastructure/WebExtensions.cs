using Hangfire;
using HeadendHQ.Web.Jobs;
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
    /// restart rather than leaving a stale schedule behind. The catalog seed and the broadcaster
    /// crawl are one-time work: they are enqueued only when <paramref name="freshDatabase"/> is set,
    /// i.e. the database was just created. On an existing database neither runs unless triggered by
    /// hand (<c>POST /catalog/sync</c>, <c>POST /catalog/broadcasters/discover</c>).
    /// </para>
    /// </summary>
    public static void UseJobs(this WebApplication app, bool freshDatabase)
    {
        var schedule = app.Configuration["NightlyJob:CronSchedule"] ?? DefaultNightlySchedule;

        RecurringJob.AddOrUpdate<NightlyJob>(
            NightlyJob.RecurringJobId,
            job => job.RunAsync(CancellationToken.None),
            schedule,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

        if (bool.TryParse(app.Configuration["NightlyJob:RunOnStartup"], out var runOnStartup) && runOnStartup)
            BackgroundJob.Enqueue<NightlyJob>(job => job.RunAsync(CancellationToken.None));

        if (freshDatabase)
        {
            BackgroundJob.Enqueue<CatalogSeedJob>(job => job.RunAsync(CancellationToken.None));
            BackgroundJob.Enqueue<BroadcasterDiscoveryJob>(job => job.RunAsync(CancellationToken.None));
        }
    }
}
