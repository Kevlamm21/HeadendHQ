using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Settings;

public record GetScheduleScrapingSettingsQuery : IQuery<ScheduleScrapingSettings>;

public class GetScheduleScrapingSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetScheduleScrapingSettingsQuery, ScheduleScrapingSettings>
{
    public async ValueTask<ScheduleScrapingSettings> Handle(GetScheduleScrapingSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new ScheduleScrapingSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("ScheduleScrapingSettings not found.");
    }
}
