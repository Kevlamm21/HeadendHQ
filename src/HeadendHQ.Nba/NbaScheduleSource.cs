using System.Text.Json;
using HeadendHQ.Core;
using HeadendHQ.Core.Browser;
using HeadendHQ.Core.Options;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.Nba.Models;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeadendHQ.Nba;

public class NbaScheduleSource(
    IMediator mediator,
    IBrowserContextFactory browserFactory,
    IOptions<ScheduleScraperOptions> options,
    ILogger<NbaScheduleSource> logger) : IScheduleSource
{
    private const string ScheduleUrl = "https://cdn.nba.com/static/json/staticData/scheduleLeagueV2_1.json";

    private static readonly Dictionary<string, StreamingService> BroadcasterMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "ESPN",         StreamingService.Espn },
            { "ABC",          StreamingService.Espn },
            { "NBA TV",       StreamingService.NbaLeaguePass },
            { "Prime Video",  StreamingService.AmazonPrime },
            { "Peacock",      StreamingService.Peacock }
        };

    public async Task<int> FetchEventsAsync(CancellationToken ct)
    {
        var opts = options.Value;

        if (opts.EnabledStreamingServices.Count == 0)
        {
            logger.LogWarning("No streaming services are enabled in ScheduleScraper config. Skipping NBA scrape.");
            return 0;
        }

        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(opts.ScrapeWindowDays);

        var json = await FetchScheduleJsonAsync(ct);
        var schedule = JsonSerializer.Deserialize<NbaScheduleRoot>(json)
            ?? throw new InvalidOperationException("NBA schedule response deserialized to null.");

        var upserted = 0;

        foreach (var gameDate in schedule.LeagueSchedule.GameDates)
        {
            foreach (var game in gameDate.Games)
            {
                if (!DateTimeOffset.TryParse(game.GameDateTimeUtc, out var startOffset))
                    continue;

                var startUtc = startOffset.UtcDateTime;

                if (startUtc < now || startUtc > windowEnd)
                    continue;

                var broadcaster = FindEnabledBroadcaster(
                    game.Broadcasters?.NationalBroadcasters ?? [],
                    opts.EnabledStreamingServices);
                if (broadcaster is null)
                    continue;

                var service = BroadcasterMap[broadcaster.BroadcasterDisplay];
                var title = $"{game.HomeTeam.TeamCity} {game.HomeTeam.TeamName} vs {game.AwayTeam.TeamCity} {game.AwayTeam.TeamName}";

                var eventUrl = service == StreamingService.NbaLeaguePass
                    ? $"https://www.nba.com/game/{game.GameId}"
                    : broadcaster.VideoLink;

                var request = new TitleRequest
                {
                    Name = title,
                    Type = TitleType.SportingEvent,
                    StreamingService = service,
                    ExternalId = game.GameId,
                    EventUrl = eventUrl,
                    Provider = "NBA.com",
                    StartUtc = startUtc,
                    EndUtc = startUtc.AddHours(3)
                };

                await mediator.Send(new CreateTitleCommand(request), ct);
                upserted++;
            }
        }

        logger.LogInformation("NBA scrape complete. {Count} events upserted in the next {Days} days.", upserted, opts.ScrapeWindowDays);
        return upserted;
    }

    private async Task<string> FetchScheduleJsonAsync(CancellationToken ct)
    {
        await using var context = await browserFactory.CreateAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36"
        }, ct);

        return await context.ApiGetAsync(ScheduleUrl, new Dictionary<string, string>
        {
            { "accept",             "*/*" },
            { "accept-language",    "en-US,en;q=0.9" },
            { "dnt",                "1" },
            { "origin",             "https://www.nba.com" },
            { "referer",            "https://www.nba.com/" },
            { "sec-ch-ua",          "\"Chromium\";v=\"148\", \"Google Chrome\";v=\"148\", \"Not/A)Brand\";v=\"99\"" },
            { "sec-ch-ua-mobile",   "?0" },
            { "sec-ch-ua-platform", "\"Windows\"" },
            { "sec-fetch-dest",     "empty" },
            { "sec-fetch-mode",     "cors" },
            { "sec-fetch-site",     "same-site" },
        }, ct);
    }

    private static NbaBroadcaster? FindEnabledBroadcaster(
        List<NbaBroadcaster> broadcasters,
        List<StreamingService> enabledServices)
    {
        foreach (var broadcaster in broadcasters)
        {
            if (BroadcasterMap.TryGetValue(broadcaster.BroadcasterDisplay, out var service)
                && enabledServices.Contains(service))
            {
                return broadcaster;
            }
        }

        return null;
    }
}
