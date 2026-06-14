using System.Text.Json;
using HeadendHQ.Core;
using HeadendHQ.Core.Assets;
using HeadendHQ.Core.Assets.Specifications;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.Nba.Models;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace HeadendHQ.Nba;

public class NbaScheduleScraper(
    IMediator mediator,
    IReadModel readModel,
    StreamingServiceMapper streamingServiceMapper,
    ILogger<NbaScheduleScraper> logger) : IScheduleScraper
{
    private const string ScheduleUrl = "https://cdn.nba.com/static/json/staticData/scheduleLeagueV2_1.json";

    public async Task<int> FetchEventsAsync(int scrapeWindowDays, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(scrapeWindowDays);

        var json = await FetchScheduleJsonAsync(ct);
        var schedule = JsonSerializer.Deserialize<NbaScheduleRoot>(json)
            ?? throw new InvalidOperationException("NBA schedule response deserialized to null.");

        var leagueAsset = await readModel.SingleOrDefault(new LeagueAssetByLeagueVariantSpec(League.Nba, "Default"), ct);
        var wordMark = await readModel.SingleOrDefault(new WordMarkByLeagueVariantSpec(League.Nba, "Original"), ct);

        var teamAssets = (await readModel.All<TeamAsset>(ct))
            .Where(a => a.League == League.Nba)
            .ToDictionary(a => a.TeamName);

        var streamingAssets = (await readModel.All<StreamingServiceAsset>(ct))
            .ToDictionary(a => a.Service);

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

                StreamingService? resolvedService = null;
                string? resolvedLink = null;

                foreach (var broadcaster in game.Broadcasters?.NationalBroadcasters ?? [])
                {
                    var service = await streamingServiceMapper.MapAsync(broadcaster.BroadcasterDisplay, ct);
                    if (service is null) continue;

                    if (!string.IsNullOrEmpty(broadcaster.VideoLink))
                    {
                        resolvedService = service;
                        resolvedLink = broadcaster.VideoLink;
                        break;
                    }

                    resolvedService ??= service;
                }

                if (resolvedService is null)
                    continue;

                var homeTeam = $"{game.HomeTeam.TeamCity} {game.HomeTeam.TeamName}";
                var awayTeam = $"{game.AwayTeam.TeamCity} {game.AwayTeam.TeamName}";

                var metadata = BuildMetadata(
                    homeTeam, awayTeam, resolvedService.Value,
                    startUtc.Year, BuildTagline(game), $"{awayTeam} at {homeTeam}.",
                    leagueAsset?.Id, wordMark?.Id,
                    teamAssets, streamingAssets);

                var request = new TitleRequest
                {
                    Name = $"{homeTeam} vs {awayTeam}",
                    Type = TitleType.SportingEvent,
                    StreamingService = resolvedService.Value,
                    ExternalId = game.GameId,
                    EventUrl = resolvedLink,
                    Provider = "NBA.com",
                    StartUtc = startUtc,
                    EndUtc = startUtc.AddHours(3),
                    Metadata = metadata,
                };

                await mediator.Send(new CreateTitleCommand(request), ct);
                upserted++;
            }
        }

        logger.LogInformation("NBA scrape complete. {Count} events upserted.", upserted);
        return upserted;
    }

    private static TitleMetadata BuildMetadata(
        string homeTeam, string awayTeam, StreamingService streamingService,
        int year, string tagline, string plot,
        int? leagueAssetId, int? wordMarkId,
        Dictionary<string, TeamAsset> teamAssets,
        Dictionary<StreamingService, StreamingServiceAsset> streamingAssets)
    {
        teamAssets.TryGetValue(homeTeam, out var homeAsset);
        teamAssets.TryGetValue(awayTeam, out var awayAsset);
        streamingAssets.TryGetValue(streamingService, out var streamingAsset);

        return new TitleMetadata
        {
            Studio = "NBA",
            Genres = ["Sports", "Basketball", "NBA"],
            Sets = ["NBA"],
            Year = year,
            Tagline = tagline,
            Plot = plot,
            HomeTeamAssetId = homeAsset?.Id,
            AwayTeamAssetId = awayAsset?.Id,
            LeagueAssetId = leagueAssetId,
            StreamingServiceAssetId = streamingAsset?.Id,
            WordMarkId = wordMarkId,
        };
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
                { "origin",             "https://www.nba.com" },
                { "referer",            "https://www.nba.com/" },
                { "sec-ch-ua",          "\"Chromium\";v=\"148\", \"Google Chrome\";v=\"148\", \"Not/A)Brand\";v=\"99\"" },
                { "sec-ch-ua-mobile",   "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest",     "empty" },
                { "sec-fetch-mode",     "cors" },
                { "sec-fetch-site",     "same-site" },
            }
        });

        ct.ThrowIfCancellationRequested();

        if (!response.Ok)
            throw new InvalidOperationException($"NBA CDN returned HTTP {response.Status}.");

        return await response.TextAsync();
    }

    private static string BuildTagline(NbaGame game)
    {
        if (!string.IsNullOrWhiteSpace(game.GameLabel))
        {
            return !string.IsNullOrWhiteSpace(game.SeriesText)
                ? $"{game.GameLabel}: {game.SeriesText}"
                : game.GameLabel;
        }

        return "NBA Regular Season";
    }
}
