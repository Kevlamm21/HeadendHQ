using HeadendHQ.DummyVideo.EventHandlers;
using Mediator;

namespace HeadendHQ.Web.SportingEvents;

public static class DummyVideoEndpoints
{
    public static void MapDummyVideoEndpoints(this WebApplication app)
    {
        app.MapPost("/videos/create", async (IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CreateDummyVideosCommand(), ct);
            return Results.Ok(new { message = "Video creation complete." });
        })
        .WithTags("Dummy Videos")
        .WithName("CreateDummyVideos")
        .WithSummary("Create today's dummy videos")
        .WithDescription("Creates dummy video files for all sporting events occurring today that do not yet have one.");

        app.MapPost("/videos/cleanup", async (IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CleanupExpiredVideosCommand(), ct);
            return Results.Ok(new { message = "Video cleanup complete." });
        })
        .WithTags("Dummy Videos")
        .WithName("CleanupExpiredVideos")
        .WithSummary("Clean up expired dummy videos")
        .WithDescription("Deletes dummy video files for sporting events that have already ended.");
    }
}
