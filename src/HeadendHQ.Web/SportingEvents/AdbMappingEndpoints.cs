using HeadendHQ.Core.SportingEvents.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.SportingEvents;

public static class AdbMappingEndpoints
{
    public static void MapAdbMappingEndpoints(this WebApplication app)
    {
        app.MapPost("/adb/map", async (IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MapAdbCommandsCommand(), ct);
            return Results.Ok(new { message = "ADB mapping complete." });
        })
        .WithTags("ADB Mapping")
        .WithName("MapAdbCommands")
        .WithSummary("Map pending ADB commands")
        .WithDescription("Assigns ADB launch commands to any sporting events that do not yet have one.");
    }
}
