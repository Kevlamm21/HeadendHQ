using HeadendHQ.Core.Events.CommandHandlers;
using Mediator;

namespace HeadendHQ.Web.Api;

/// <summary>
/// Sporting events: the scraped schedule, ahead of anything being produced for Jellyfin.
/// <para>
/// Distinct from <c>/titles</c> on purpose. An event is what is happening; a title is the artifact we
/// build for one that is due. Events cover the whole scrape window, titles only today's.
/// </para>
/// </summary>
public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("/events").WithTags("Sporting Events");

        events.MapGet("/", async (
            DateTime? from, DateTime? to, bool? awaitingTitle, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetSportingEventsQuery(from, to, awaitingTitle), ct)))
            .WithName("GetSportingEvents")
            .WithSummary("List sporting events")
            .WithDescription("The scraped schedule. Filter by UTC start range, or to those still awaiting a title.");

        events.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            await mediator.Send(new GetSportingEventByIdQuery(id), ct) is { } sportingEvent
                ? Results.Ok(sportingEvent)
                : Results.NotFound())
            .WithName("GetSportingEventById")
            .WithSummary("Get a sporting event");

        events.MapPost("/scrape", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ImportScheduleCommand(), ct)))
            .WithName("ImportSchedule")
            .WithSummary("Scrape the schedule")
            .WithDescription("Pulls the schedule window from every source into sporting events, then queues each new event's detail lookup. Titles are not created here — that happens for the day's events, once detail has been collected.");

        events.MapPost("/{id:guid}/detail", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CollectEventDetailCommand(id), ct);
            return Results.Ok(await mediator.Send(new GetSportingEventByIdQuery(id), ct));
        })
            .WithName("CollectEventDetail")
            .WithSummary("Collect an event's detail now")
            .WithDescription("Runs the venue, note, series and cast lookup inline instead of waiting for the queued job.");

        events.MapPost("/titles", async (int? leadDays, IMediator mediator, CancellationToken ct) =>
            Results.Ok(new { created = await mediator.Send(new CreateTitlesForTodayCommand(leadDays ?? 0), ct) }))
            .WithName("CreateTitlesForToday")
            .WithSummary("Produce titles for today's events")
            .WithDescription("Maps every event due today that has finished collecting detail into a title. Pass leadDays to produce further ahead.");

        events.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteSportingEventCommand(id), ct);
            return Results.NoContent();
        })
            .WithName("DeleteSportingEvent")
            .WithSummary("Delete a sporting event");

        events.MapDelete("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var deleted = await mediator.Send(new DeleteAllSportingEventsCommand(), ct);
            return Results.Ok(new { deleted });
        })
            .WithName("DeleteAllSportingEvents")
            .WithSummary("Delete all sporting events")
            .WithDescription("Debugging aid. Removes every sporting event so the schedule can be replayed from scratch.");
    }
}
