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

        iptv.MapGet("/programmes", async (
            string guideNumber, DateTime? from, DateTime? to, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetIptvProgrammesQuery(guideNumber, from, to), ct)))
        .WithName("GetIptvProgrammes")
        .WithSummary("What is airing on one channel")
        .WithDescription(
            "Guide entries on a channel that overlap the window, from the parsed XMLTV document. " +
            "Times are UTC; from defaults to now and to defaults to 24 hours later. " +
            "This is the data the regional check reads to decide whether a local affiliate is really carrying a given game.");

        iptv.MapPost("/programmes/parse", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { parsed = await mediator.Send(new ParseIptvGuideCommand(), ct) }))
        .WithName("ParseIptvGuide")
        .WithSummary("Re-parse the cached programme guide")
        .WithDescription(
            "Replaces every stored guide entry from the cached XMLTV document. Runs nightly after the lineup refresh; " +
            "only channels that resolve to a lineup guide number are kept.");
    }
}
