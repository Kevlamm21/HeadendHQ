using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Settings;

public class ScheduleScrapingSettingsSpec : ISpecification<ScheduleScrapingSettings>
{
    public IQueryable<ScheduleScrapingSettings> Apply(IQueryable<ScheduleScrapingSettings> q) => q;
}
