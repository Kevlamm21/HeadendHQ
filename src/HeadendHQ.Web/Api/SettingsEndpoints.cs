using HeadendHQ.Core.Settings;
using HeadendHQ.HdHomerun.Settings;
using HeadendHQ.VodLauncher.Settings;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace HeadendHQ.Web.Api;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings").WithTags("Settings");

        group.MapGet("/global", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetGlobalSettingsQuery(), ct)))
            .WithName("GetGlobalSettings")
            .WithSummary("Get global settings")
            .WithDescription("Returns global settings including enabled streaming services and title retention period.");

        group.MapPatch("/global", async (
            [FromBody] UpdateGlobalSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateGlobalSettings")
            .WithSummary("Update global settings")
            .WithDescription("Partially updates global settings. Only non-null fields are applied.");

        group.MapGet("/schedule-scraping", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetScheduleScrapingSettingsQuery(), ct)))
            .WithName("GetScheduleScrapingSettings")
            .WithSummary("Get schedule scraping settings")
            .WithDescription("Returns schedule scraping settings including how many days ahead to scrape.");

        group.MapPatch("/schedule-scraping", async (
            [FromBody] UpdateScheduleScrapingSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateScheduleScrapingSettings")
            .WithSummary("Update schedule scraping settings")
            .WithDescription("Partially updates schedule scraping settings. Only non-null fields are applied.");

        // What used to be "sport preferences" is now follow state on the catalog itself:
        // PATCH /catalog/leagues/{id} and PATCH /catalog/teams/{id}.

        // Source pacing (request rate, spacing, jitter, concurrency, per-run budget, user agent) is
        // not a per-install preference, so it lives in HeadendHQ.Core.Settings.SourceSettings as
        // constants rather than behind an endpoint.

        group.MapGet("/vod-launcher", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetVodLauncherSettingsQuery(), ct)))
            .WithName("GetVodLauncherSettings")
            .WithSummary("Get VOD launcher settings")
            .WithDescription("Returns VOD launcher settings including library paths per title type.");

        group.MapPatch("/vod-launcher", async (
            [FromBody] UpdateVodLauncherSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateVodLauncherSettings")
            .WithSummary("Update VOD launcher settings")
            .WithDescription("Partially updates VOD launcher settings. Only non-null fields are applied.");

        group.MapGet("/iptv", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetHdHomerunSettingsQuery(), ct)))
            .WithName("GetIptvSettings")
            .WithSummary("Get IPTV device settings")
            .WithDescription("Returns the IPTV device settings, including the discover.json URL used for the guide and lineup.");

        group.MapPatch("/iptv", async (
            [FromBody] UpdateHdHomerunSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateIptvSettings")
            .WithSummary("Update IPTV device settings")
            .WithDescription("Partially updates the IPTV device settings. Only non-null fields are applied.");

        return app;
    }
}
