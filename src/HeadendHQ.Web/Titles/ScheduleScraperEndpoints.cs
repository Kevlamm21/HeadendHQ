using HeadendHQ.ScheduleScraping;
using Mediator;

namespace HeadendHQ.Web.Titles;

public static class ScheduleScraperEndpoints
{
    public static void MapScheduleScraperEndpoints(this WebApplication app)
    {
        app.MapPost("/titles/scrape", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ScrapeSchedulesCommand(), ct);
            return Results.Ok(result);
        })
        .WithTags("Schedule Scraping")
        .WithName("TriggerScrape")
        .WithSummary("Trigger schedule scrape")
        .WithDescription("Manually triggers a full schedule scrape across all configured sources. Creates or upserts titles, then enqueues ADB mapping and VOD creation per title in the background.");
    }
}
