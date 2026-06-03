using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IScheduleScraper
{
    Task<int> FetchEventsAsync(CancellationToken ct);
}
public static class ScheduleScraperExtensions
{
    public static DateTime GetWindowEnd(ScheduleScraperSettings settings) =>
        DateTime.UtcNow.AddDays(settings.ScrapeWindowDays);

    public static StreamingService? MapBroadcaster(string broadcasterDisplay)
    {
        var normalized = broadcasterDisplay.ToLowerInvariant().Replace(" ", "");
        return normalized switch
        {
            "espn" or "abc" => StreamingService.Espn,
            "nbatv" => StreamingService.NbaLeaguePass,
            "primevideo" or "amazon" or "amazonprime" => StreamingService.AmazonPrime,
            "peacock" or "nbc" => StreamingService.Peacock,
            _ => null
        };
    }

    public static ResolvedBroadcaster? FindEnabledBroadcaster(
        IEnumerable<Broadcaster> broadcasters,
        ScheduleScraperSettings settings)
    {
        ResolvedBroadcaster? fallback = null;

        foreach (var broadcaster in broadcasters)
        {
            var service = MapBroadcaster(broadcaster.DisplayName);
            if (service is null || !settings.EnabledStreamingServices.Contains(service.Value))
                continue;

            if (!string.IsNullOrEmpty(broadcaster.VideoLink))
                return new ResolvedBroadcaster { streamingService = service.Value, VideoLink = broadcaster.VideoLink };

            fallback ??= new ResolvedBroadcaster { streamingService = service.Value };
        }

        return fallback;
    }
}
public record Broadcaster
{
    public required string DisplayName { get; init; }
    public string? VideoLink { get; init; }
}
public record ResolvedBroadcaster
{
    public required StreamingService streamingService { get; init; }
    public string? VideoLink { get; init; }
}