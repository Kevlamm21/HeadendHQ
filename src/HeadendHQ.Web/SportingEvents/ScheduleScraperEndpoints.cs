using HeadendHQ.Core.SportingEvents.CommandHandlers;
using HeadendHQ.Nba;
using Mediator;

namespace HeadendHQ.Web.SportingEvents;

public static class ScheduleScraperEndpoints
{
    public static void MapScheduleScraperEndpoints(this WebApplication app)
    {
        app.MapPost("/sporting-events/scrape", async (IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ScrapeSchedulesCommand(), ct);
            return Results.Ok(new { message = "Scrape completed." });
        })
        .WithTags("Sporting Events")
        .WithName("TriggerScrape")
        .WithSummary("Trigger schedule scrape")
        .WithDescription("Manually triggers a full schedule scrape across all sources, ADB mapping, and cleanup.");

        app.MapGet("/debug/espn-resolve/{calendarEventId}", async (int calendarEventId, CancellationToken ct) =>
        {
            var fakeSmartLink = $"https://espn.smart.link/debug?gameId={calendarEventId}";
            var resolved = await EspnLinkResolver.ResolveAsync(fakeSmartLink, ct);
            return resolved is not null
                ? Results.Ok(new { calendarEventId, resolvedUrl = resolved })
                : Results.NotFound(new { calendarEventId, error = "Stream ID not found. evntId may be missing from the ESPN page, or strms array is empty." });
        })
        .WithTags("Debug")
        .WithName("DebugEspnResolve")
        .WithSummary("Debug ESPN link resolver")
        .WithDescription("Runs a calendar event ID through the ESPN link resolver and returns the resolved stream URL.");
    }
}
