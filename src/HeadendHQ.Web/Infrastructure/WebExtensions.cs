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
        builder.Services.AddScoped<TeamLogoJob>();
    }

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
