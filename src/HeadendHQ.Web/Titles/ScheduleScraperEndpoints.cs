using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.Nba;
using Mediator;

namespace HeadendHQ.Web.Titles;

public static class ScheduleScraperEndpoints
{
    public static void MapScheduleScraperEndpoints(this WebApplication app)
    {
        app.MapPost("/sporting-events/scrape", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ScrapeSchedulesCommand(), ct);
            return Results.Ok(result);
        })
        .WithTags("Sporting Events")
        .WithName("TriggerScrape")
        .WithSummary("Trigger schedule scrape")
        .WithDescription("Manually triggers a full schedule scrape across all sources, ADB mapping, and cleanup.");
    }
}
