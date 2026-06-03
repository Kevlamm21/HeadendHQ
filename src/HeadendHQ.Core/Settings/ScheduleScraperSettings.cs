using HeadendHQ.Core.Titles;

namespace HeadendHQ.Core.Settings;

public class ScheduleScraperSettings
{
    public int Id { get; private set; }
    public List<StreamingService> EnabledStreamingServices { get; private set; } = [];
    public int ScrapeWindowDays { get; private set; } = 7;
    public int CleanupRetentionDays { get; private set; } = 30;
    public string CronSchedule { get; private set; } = "0 2 * * *";
    public bool RunOnStartup { get; private set; } = false;

    public void Configure(
        List<StreamingService> enabledStreamingServices,
        int scrapeWindowDays,
        int cleanupRetentionDays,
        string cronSchedule,
        bool runOnStartup)
    {
        EnabledStreamingServices = enabledStreamingServices;
        ScrapeWindowDays = scrapeWindowDays;
        CleanupRetentionDays = cleanupRetentionDays;
        CronSchedule = cronSchedule;
        RunOnStartup = runOnStartup;
    }
}
