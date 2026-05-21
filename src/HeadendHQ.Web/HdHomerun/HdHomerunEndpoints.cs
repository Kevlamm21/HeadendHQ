using HeadendHQ.Core.HdHomerun.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.HdHomerun;

public static class HdHomerunEndpoints
{
    public static void MapHdHomerunEndpoints(this WebApplication app)
    {
        app.MapGet("/hdhr/xmltv", async (IMediator mediator, CancellationToken ct) =>
        {
            var content = await mediator.Send(new GetHdHomerunXmltvQuery(), ct);

            if (content is null)
                return Results.NotFound(new { message = "XMLTV data not yet available. It will be populated on the next refresh." });

            return Results.Content(content, "application/xml");
        })
        .WithTags("HDHomeRun")
        .WithName("GetHdHomerunXmltv")
        .WithSummary("Get HDHomeRun XMLTV")
        .WithDescription("Returns the cached HDHomeRun XMLTV content. Refreshed nightly at 2:30 AM and on startup.");
    }
}
