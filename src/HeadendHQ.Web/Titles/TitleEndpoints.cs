using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Titles;

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
        {
            var titles = await mediator.Send(new GetTitlesQuery(from, to, type), ct);
            return Results.Ok(titles);
        })
        .WithTags("Titles")
        .WithName("GetTitles")
        .WithSummary("List titles")
        .WithDescription("Returns titles. Optionally filter by date range and type.");

        app.MapGet("/titles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var title = await mediator.Send(new GetTitleByIdQuery(id), ct);

            return title is null
                ? Results.NotFound(new { message = $"Title {id} not found." })
                : Results.Ok(title);
        })
        .WithTags("Titles")
        .WithName("GetTitleById")
        .WithSummary("Get title")
        .WithDescription("Returns a single title by ID.");

        app.MapGet("/titles/launch-command", async (string name, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var adbCommand = await mediator.Send(new GetLaunchCommand(name), ct);
                return Results.Ok(new { name, adbCommand });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
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
        .WithDescription("Creates a new title.");

        app.MapPut("/titles/{id:guid}", async (Guid id, TitleRequest request, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var title = await mediator.Send(new UpdateTitleCommand(id, request), ct);
                return Results.Ok(title);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Titles")
        .WithName("UpdateTitle")
        .WithSummary("Update title")
        .WithDescription("Updates an existing title. Changing the EventUrl will clear the mapped ADB command.");

        app.MapDelete("/titles/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.Send(new DeleteTitleCommand(id), ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Titles")
        .WithName("DeleteTitle")
        .WithSummary("Delete title")
        .WithDescription("Deletes a title by ID.");
    }
}
