using HeadendHQ.SixLabors.EventHandlers;
using Mediator;

namespace HeadendHQ.Web.Titles;

public static class ArtworkEndpoints
{
    public static void MapArtworkEndpoints(this WebApplication app)
    {
        app.MapPost("/artwork/create", async (IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CreateArtworkCommand(), ct);
            return Results.Ok(new { message = "Artwork creation complete." });
        })
        .WithTags("Artwork")
        .WithName("CreateArtwork")
        .WithSummary("Create artwork")
        .WithDescription("Creates poster and thumbnail artwork for all titles that have a video but no artwork yet.");
    }
}
