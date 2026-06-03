namespace HeadendHQ.HdHomerun;

public class HdHomerunSettings
{
    public int Id { get; private set; }
    public string DeviceUrl { get; private set; } = string.Empty;
    public string CronSchedule { get; private set; } = "0 3 * * *";
    public bool RunOnStartup { get; private set; } = true;

    public void Configure(string deviceUrl, string cronSchedule, bool runOnStartup)
    {
        DeviceUrl = deviceUrl;
        CronSchedule = cronSchedule;
        RunOnStartup = runOnStartup;
    }
}
