using HeadendHQ.Core.Iptv.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Api;

public static class IptvEndpoints
{
    public static void MapIptvEndpoints(this WebApplication app)
    {
        var iptv = app.MapGroup("/iptv").WithTags("IPTV");

        iptv.MapGet("/guide", async (IMediator mediator, CancellationToken ct) =>
        {
            var content = await mediator.Send(new GetIptvGuideQuery(), ct);

            return content is null
                ? Results.NotFound(new { message = "Programme guide not yet available. It is populated on the nightly refresh." })
                : Results.Content(content, "application/xml");
        })
        .WithName("GetIptvGuide")
        .WithSummary("Get the cached IPTV programme guide (XMLTV)")
        .WithDescription("Returns the cached XMLTV document. Refreshed on the nightly job.");

        iptv.MapGet("/lineup", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetIptvLineupQuery(), ct)))
        .WithName("GetIptvLineup")
        .WithSummary("Get the parsed IPTV channel lineup")
        .WithDescription("Guide number, call sign, display name and icon for each tunable channel, read from the device's lineup.json.");
    }
}
