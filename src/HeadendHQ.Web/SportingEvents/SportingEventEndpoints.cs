using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Core.SportingEvents.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.SportingEvents;

public static class SportingEventEndpoints
{
    public static void MapSportingEventEndpoints(this WebApplication app)
    {
        app.MapGet("/sporting-events", async (
            IMediator mediator,
            DateTime? from,
            DateTime? to,
            Sport? sport,
            CancellationToken ct) =>
        {
            var events = await mediator.Send(new GetSportingEventsQuery(from, to, sport), ct);
            return Results.Ok(events);
        })
        .WithTags("Sporting Events")
        .WithName("GetSportingEvents")
        .WithSummary("List sporting events")
        .WithDescription("Returns scheduled sporting events. Optionally filter by date range and sport.");

        app.MapGet("/sporting-events/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var evt = await mediator.Send(new GetSportingEventByIdQuery(id), ct);

            return evt is null
                ? Results.NotFound(new { message = $"Sporting event {id} not found." })
                : Results.Ok(evt);
        })
        .WithTags("Sporting Events")
        .WithName("GetSportingEventById")
        .WithSummary("Get sporting event")
        .WithDescription("Returns a single sporting event by ID.");

        app.MapGet("/sporting-events/launch-command", async (string title, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var adbCommand = await mediator.Send(new GetLaunchCommand(title), ct);
                return Results.Ok(new { title, adbCommand });
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
        .WithTags("Sporting Events")
        .WithName("GetLaunchCommand")
        .WithSummary("Get ADB launch command")
        .WithDescription("Returns the ADB launch command for a sporting event matching the given title.");

        app.MapPost("/sporting-events", async (SportingEventRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var evt = await mediator.Send(new CreateSportingEventCommand(request), ct);
            return Results.Created($"/sporting-events/{evt.Id}", evt);
        })
        .WithTags("Sporting Events")
        .WithName("CreateSportingEvent")
        .WithSummary("Create sporting event")
        .WithDescription("Creates a new sporting event.");

        app.MapPut("/sporting-events/{id:int}", async (int id, SportingEventRequest request, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var evt = await mediator.Send(new UpdateSportingEventCommand(id, request), ct);
                return Results.Ok(evt);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Sporting Events")
        .WithName("UpdateSportingEvent")
        .WithSummary("Update sporting event")
        .WithDescription("Updates an existing sporting event. Changing the EventUrl will clear the mapped ADB command.");

        app.MapDelete("/sporting-events/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                await mediator.Send(new DeleteSportingEventCommand(id), ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithTags("Sporting Events")
        .WithName("DeleteSportingEvent")
        .WithSummary("Delete sporting event")
        .WithDescription("Deletes a sporting event by ID.");
    }
}
