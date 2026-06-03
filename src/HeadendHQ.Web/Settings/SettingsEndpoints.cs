using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles;
using HeadendHQ.DummyVideo;
using HeadendHQ.DummyVideo.Settings;
using HeadendHQ.HdHomerun;
using HeadendHQ.HdHomerun.Settings;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace HeadendHQ.Web.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings").WithTags("Settings");

        group.MapGet("/schedule-scraper", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetScheduleScraperSettingsQuery(), ct)))
            .WithName("GetScheduleScraperSettings")
            .WithSummary("Get schedule scraper settings");

        group.MapPut("/schedule-scraper", async (
            [FromBody] UpdateScheduleScraperSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateScheduleScraperSettings")
            .WithSummary("Update schedule scraper settings");

        group.MapGet("/dummy-video", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetDummyVideoSettingsQuery(), ct)))
            .WithName("GetDummyVideoSettings")
            .WithSummary("Get dummy video settings");

        group.MapPut("/dummy-video", async (
            [FromBody] UpdateDummyVideoSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateDummyVideoSettings")
            .WithSummary("Update dummy video settings");

        group.MapGet("/hdhomerun", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetHdHomerunSettingsQuery(), ct)))
            .WithName("GetHdHomerunSettings")
            .WithSummary("Get HDHomeRun settings");

        group.MapPut("/hdhomerun", async (
            [FromBody] UpdateHdHomerunSettingsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(command, ct)))
            .WithName("UpdateHdHomerunSettings")
            .WithSummary("Update HDHomeRun settings");

        return app;
    }
}
