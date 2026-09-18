using HeadendHQ.Core.Media.CommandHandlers;
using Mediator;
using Microsoft.Net.Http.Headers;

namespace HeadendHQ.Web.Api;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this WebApplication app)
    {
        app.MapGet("/media/images/{id:int}", async (int id, HttpContext http, IMediator mediator, CancellationToken ct) =>
        {
            var image = await mediator.Send(new GetImageQuery(id), ct);

            if (image is null)
                return Results.NotFound();

            var etag = new EntityTagHeaderValue($"\"{image.Sha256}\"");
            http.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            return Results.File(image.Bytes, image.ContentType, entityTag: etag);
        })
        .WithTags("Media")
        .WithName("GetImage")
        .WithSummary("Get image bytes")
        .WithDescription("Returns a stored image. Served with a strong ETag and an immutable cache header.");
    }
}
