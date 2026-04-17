using HeadendHQ.Core.HdHomerun;

namespace HeadendHQ.Web.HdHomerun;

public static class HdHomerunEndpoints
{
    public static void MapHdHomerunEndpoints(this WebApplication app)
    {
        app.MapGet("/hdhr/xmltv", async (IHdHomerunService service, CancellationToken ct) =>
        {
            var content = await service.GetXmltvContentAsync(ct);

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
