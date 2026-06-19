namespace HeadendHQ.Core;

public interface IScheduleScraper
{
    string Provider { get; }
    Task<int> FetchEventsAsync(int scrapeWindowDays, CancellationToken ct);
}

