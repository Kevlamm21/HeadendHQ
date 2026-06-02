using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Options;

public class ScheduleScraperOptions
{
    public const string SectionName = "ScheduleScraper";

    public List<StreamingService> EnabledStreamingServices { get; set; } = [];
    public int ScrapeWindowDays { get; set; } = 7;
    public int CleanupRetentionDays { get; set; } = 30;
}
