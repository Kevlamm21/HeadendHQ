using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core;

public interface IScheduleScraper
{
    Task<int> FetchEventsAsync(int scrapeWindowDays, CancellationToken ct);
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
