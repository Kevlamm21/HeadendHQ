namespace HeadendHQ.Core;

public interface IScheduleScraper
{
    Task<int> FetchEventsAsync(int scrapeWindowDays, CancellationToken ct);
}

