namespace HeadendHQ.ScheduleScraping;

public class ScheduleScrapingSettings
{
    public int Id { get; private set; }
    public int ScrapeWindowDays { get; private set; } = 7;

    public void Configure(int? scrapeWindowDays)
    {
        if (scrapeWindowDays is not null) ScrapeWindowDays = scrapeWindowDays.Value;
    }
}
