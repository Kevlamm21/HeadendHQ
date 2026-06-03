using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Settings;

public class ScheduleScraperSettingsSpec : ISpecification<ScheduleScraperSettings>
{
    public IQueryable<ScheduleScraperSettings> Apply(IQueryable<ScheduleScraperSettings> q) => q;
}
