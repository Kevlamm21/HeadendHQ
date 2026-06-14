using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles;
using HeadendHQ.ScheduleScraping;
using HeadendHQ.ScheduleScraping.Settings;
using HeadendHQ.VodLauncher;
using HeadendHQ.VodLauncher.Settings;
using HeadendHQ.HdHomerun;
using HeadendHQ.HdHomerun.Settings;
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

        group.MapGet("/hdhomerun", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetHdHomerunSettingsQuery(), ct)))
            .WithName("GetHdHomerunSettings")
            .WithSummary("Get HDHomeRun settings")
            .WithDescription("Returns HDHomeRun settings including the device URL used for XMLTV EPG fetching.");

        group.MapPatch("/hdhomerun", async (
            [FromBody] UpdateHdHomerunSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateHdHomerunSettings")
            .WithSummary("Update HDHomeRun settings")
            .WithDescription("Partially updates HDHomeRun settings. Only non-null fields are applied.");

        return app;
    }
}
