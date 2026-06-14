using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.VodLauncher.EventHandlers;
using Mediator;

namespace HeadendHQ.Web.Api;

public static class TitleEndpoints
{
    public static void MapTitleEndpoints(this WebApplication app)
    {
        app.MapGet("/titles", async (
            IMediator mediator,
            DateTime? from,
            DateTime? to,
            TitleType? type,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetTitlesQuery(from, to, type), ct)))
        .WithTags("Titles")
        .WithName("GetTitles")
        .WithSummary("List titles")
        .WithDescription("Returns titles. Optionally filter by date range and type.");

        app.MapGet("/titles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetTitleByIdQuery(id), ct)))
        .WithTags("Titles")
        .WithName("GetTitleById")
        .WithSummary("Get title")
        .WithDescription("Returns a single title by ID.");

        app.MapGet("/titles/launch-command", async (string name, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetLaunchCommand(name), ct)))
        .WithTags("Titles")
        .WithName("GetLaunchCommand")
        .WithSummary("Get ADB launch command")
        .WithDescription("Returns the ADB launch command for a title matching the given name.");

        app.MapPost("/titles", async (TitleRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var title = await mediator.Send(new CreateTitleCommand(request), ct);
            return Results.Created($"/titles/{title.Id}", title);
        })
        .WithTags("Titles")
        .WithName("CreateTitle")
        .WithSummary("Create title")
        .WithDescription("Creates a new title and enqueues ADB mapping and VOD file creation in the background.");

        app.MapPatch("/titles/{id:guid}", async (Guid id, UpdateTitleRequest request, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateTitleCommand(id, request), ct)))
        .WithTags("Titles")
        .WithName("UpdateTitle")
        .WithSummary("Update title")
        .WithDescription("Updates an existing title. Changing the EventUrl will clear the mapped ADB command.");

        app.MapDelete("/titles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteTitleCommand(id), ct);
            return Results.NoContent();
        })
        .WithTags("Titles")
        .WithName("DeleteTitle")
        .WithSummary("Delete title")
        .WithDescription("Deletes a title by ID.");

        app.MapGroup("/titles/{id}/images")
            .WithTags("Titles")
            .MapPost("", async (
                Guid id,
                IFormFile? poster,
                IFormFile? background,
                IFormFile? thumbnail,
                IFormFile? wordmark,
                IMediator mediator,
                CancellationToken ct) =>
            {
                await mediator.Send(new UploadTitleImagesCommand(
                    id,
                    poster?.OpenReadStream(),
                    background?.OpenReadStream(),
                    thumbnail?.OpenReadStream(),
                    wordmark?.OpenReadStream()), ct);

                return Results.NoContent();
            })
            .DisableAntiforgery()
            .WithName("UploadTitleImages")
            .WithSummary("Upload title artwork")
            .WithDescription("Uploads poster, background, thumbnail, and/or wordmark images for a title. Files are normalized and saved to the title's VOD folder.");
    }
}
