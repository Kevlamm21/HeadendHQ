using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Settings;

public record GetScheduleScraperSettingsQuery : IQuery<ScheduleScraperSettings>;

public class GetScheduleScraperSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetScheduleScraperSettingsQuery, ScheduleScraperSettings>
{
    public async ValueTask<ScheduleScraperSettings> Handle(GetScheduleScraperSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new ScheduleScraperSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("ScheduleScraperSettings not found.");
    }
}
