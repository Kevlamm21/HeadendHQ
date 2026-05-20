using System.Text.Json;
using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Core.SportingEvents.CommandHandlers;
using HeadendHQ.Nba.Models;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeadendHQ.Nba;

public class NbaScheduleSource(
    HttpClient httpClient,
    IMediator mediator,
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

    public async Task FetchEventsAsync(CancellationToken ct)
    {
        var opts = options.Value;

        if (opts.EnabledStreamingServices.Count == 0)
        {
            logger.LogWarning("No streaming services are enabled in ScheduleScraper config. Skipping NBA scrape.");
            return;
        }

        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(opts.ScrapeWindowDays);

        var json = await httpClient.GetStringAsync(ScheduleUrl, ct);
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
                var eventUrl = await ResolveEventUrlAsync(service, broadcaster.VideoLink, game.GameId, ct);
                if (eventUrl is null)
                    continue;

                var request = new SportingEventRequest
                {
                    Title = $"{game.HomeTeam.TeamCity} {game.HomeTeam.TeamName} vs. {game.AwayTeam.TeamCity} {game.AwayTeam.TeamName}",
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
                return await EspnLinkResolver.ResolveAsync(httpClient, videoLink, ct);
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
