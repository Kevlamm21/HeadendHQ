using HeadendHQ.Core.Streaming.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Api;

public static class StreamingEndpoints
{
    public static void MapStreamingEndpoints(this WebApplication app)
    {
        var streaming = app.MapGroup("/streaming-services").WithTags("Streaming Services");

        streaming.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetStreamingServicesQuery(), ct)))
            .WithName("GetStreamingServices")
            .WithSummary("List streaming services")
            .WithDescription(
                "One per deep-link provider HeadendHQ can launch and one per HDHomeRun lineup channel. " +
                "Services are seeded from code on startup and after the nightly lineup refresh.");

        streaming.MapPatch("/{id:int}", async (
            int id, UpdateStreamingServiceRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateStreamingServiceCommand(
                id, body.IsEnabled, body.SetLogoBroadcaster, body.LogoBroadcasterId), ct)))
            .WithName("UpdateStreamingService")
            .WithSummary("Enable or disable a streaming service, or borrow a broadcaster's logo")
            .WithDescription(
                "Assignments to a disabled service are skipped. Send setLogoBroadcaster=true with logoBroadcasterId to " +
                "use that broadcaster's selected logo on posters when no logo has been uploaded; omit the id to clear it.");

        streaming.MapPut("/{id:int}/logos/{variant}", async (
            int id, string variant, IFormFile logo, IMediator mediator, CancellationToken ct) =>
        {
            using var stream = new MemoryStream();
            await logo.CopyToAsync(stream, ct);
            return Results.Ok(await mediator.Send(
                new UploadStreamingServiceLogoCommand(id, variant, stream.ToArray()), ct));
        })
            .WithName("UploadStreamingServiceLogo")
            .WithSummary("Upload a streaming service logo variant")
            .WithDescription(
                "Adds or replaces the named variant. \"Default\" is used when an assignment names no variant, or names one " +
                "that hasn't been uploaded; e.g. upload \"nbc\" on Peacock and set logoVariant=nbc on NBC's Peacock assignment. " +
                "An uploaded logo takes precedence over a borrowed broadcaster logo.")
            .DisableAntiforgery();

        streaming.MapDelete("/{id:int}/logos/{variant}", async (
            int id, string variant, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { imagesDeleted = await mediator.Send(new DeleteStreamingServiceLogoCommand(id, variant), ct) }))
            .WithName("DeleteStreamingServiceLogo")
            .WithSummary("Delete a streaming service logo variant");
    }

    public record UpdateStreamingServiceRequest(
        bool? IsEnabled, bool SetLogoBroadcaster = false, int? LogoBroadcasterId = null);
}
