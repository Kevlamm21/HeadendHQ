using HeadendHQ.Core.SportingEvents.CommandHandlers;
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
    }
}
