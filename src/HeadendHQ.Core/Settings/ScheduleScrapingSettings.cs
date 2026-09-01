namespace HeadendHQ.Core.Settings;

public class ScheduleScrapingSettings
{
    public int Id { get; private set; }
    public int ScrapeWindowDays { get; private set; } = 7;

    /// <summary>
    /// Upper bound on how many players are attached to a title per team. Keeps the
    /// NFO cast readable and bounds headshot downloads when falling back to a full
    /// team roster (which can be ~50 players a side).
    /// </summary>
    public int MaxAthletesPerTeam { get; private set; } = 8;

    public void Configure(int? scrapeWindowDays, int? maxAthletesPerTeam = null)
    {
        if (scrapeWindowDays is not null) ScrapeWindowDays = scrapeWindowDays.Value;
        if (maxAthletesPerTeam is not null) MaxAthletesPerTeam = maxAthletesPerTeam.Value;
    }
}
