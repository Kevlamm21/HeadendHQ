namespace HeadendHQ.Core.Settings;

public class ScheduleScrapingSettings
{
    public int Id { get; private set; }
    public int ScrapeWindowDays { get; private set; } = 7;

    public int MaxAthletesPerTeam { get; private set; } = 8;

    public void Configure(int? scrapeWindowDays, int? maxAthletesPerTeam = null)
    {
        if (scrapeWindowDays is not null) ScrapeWindowDays = scrapeWindowDays.Value;
        if (maxAthletesPerTeam is not null) MaxAthletesPerTeam = maxAthletesPerTeam.Value;
    }
}
