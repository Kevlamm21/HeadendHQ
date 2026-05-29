using System.Text.Json;
using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Core.SportingEvents.CommandHandlers;
using HeadendHQ.Nba.Models;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace HeadendHQ.Nba;

public class NbaScheduleSource(
    IMediator mediator,
    ISportingEventRepository repository,
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

    public Sport Sport => Sport.Basketball;

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

                var existing = await repository.FindByProviderTitleStartAsync("NBA.com", title, startUtc, ct);
                var resolvedUrl = await ResolveEventUrlAsync(service, broadcaster.VideoLink, game.GameId, ct);
                var eventUrl = resolvedUrl ?? existing?.EventUrl ?? broadcaster.VideoLink;
                if (string.IsNullOrEmpty(eventUrl))
                    continue;

                var request = new SportingEventRequest
                {
                    Title = title,
                    Sport = Sport.Basketball,
                    StreamingService = service,
                    EventUrl = eventUrl,
                    Provider = "NBA.com",
                    StartUtc = startUtc,
                    EndUtc = startUtc.AddHours(3)
                };

                await mediator.Send(new CreateSportingEventCommand(request), ct);
                upserted++;
            }
        }

        logger.LogInformation("NBA scrape complete. {Count} events upserted in the next {Days} days.", upserted, opts.ScrapeWindowDays);
        return upserted;
    }

    private static async Task<string> FetchScheduleJsonAsync(CancellationToken ct)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        await using var context = await browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36"
        });

        var response = await context.APIRequest.GetAsync(ScheduleUrl, new()
        {
            Headers = new Dictionary<string, string>
            {
                { "accept",              "*/*" },
                { "accept-language",     "en-US,en;q=0.9" },
                { "dnt",                 "1" },
                { "origin",              "https://www.nba.com" },
                { "referer",             "https://www.nba.com/" },
                { "sec-ch-ua",           "\"Chromium\";v=\"148\", \"Google Chrome\";v=\"148\", \"Not/A)Brand\";v=\"99\"" },
                { "sec-ch-ua-mobile",    "?0" },
                { "sec-ch-ua-platform",  "\"Windows\"" },
                { "sec-fetch-dest",      "empty" },
                { "sec-fetch-mode",      "cors" },
                { "sec-fetch-site",      "same-site" },
            }
        });

        ct.ThrowIfCancellationRequested();

        if (!response.Ok)
            throw new InvalidOperationException($"NBA CDN returned HTTP {response.Status}.");

        return await response.TextAsync();
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

    private async Task<string?> ResolveEventUrlAsync(
        StreamingService service,
        string? videoLink,
        string gameId,
        CancellationToken ct)
    {
        if (service == StreamingService.NbaLeaguePass)
            return $"https://www.nba.com/game/{gameId}";

        if (string.IsNullOrEmpty(videoLink))
        {
            logger.LogWarning("Game {GameId} on {Service} has no video link.", gameId, service);
            return null;
        }

        if (service == StreamingService.Espn)
        {
            try
            {
                return await EspnLinkResolver.ResolveAsync(videoLink, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve ESPN smart link for game {GameId}.", gameId);
                return null;
            }
        }

        if (service == StreamingService.Peacock)
        {
            try
            {
                return await PeacockLinkResolver.ResolveAsync(videoLink, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve Peacock URL for game {GameId}.", gameId);
                return null;
            }
        }

        return videoLink;
    }
}
