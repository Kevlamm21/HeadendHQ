using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.ScheduleScraping.Settings;

public class ScheduleScrapingSettingsSpec : ISpecification<ScheduleScrapingSettings>
{
    public IQueryable<ScheduleScrapingSettings> Apply(IQueryable<ScheduleScrapingSettings> q) => q;
}

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

public record UpdateScheduleScrapingSettingsCommand(int? ScrapeWindowDays) : ICommand<ScheduleScrapingSettings>;

public class UpdateScheduleScrapingSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateScheduleScrapingSettingsCommand, ScheduleScrapingSettings>
{
    public async ValueTask<ScheduleScrapingSettings> Handle(UpdateScheduleScrapingSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new ScheduleScrapingSettingsSpec(), ct)
            ?? throw new InvalidOperationException("ScheduleScrapingSettings not found.");

        settings.Configure(command.ScrapeWindowDays);
        return settings;
    }
}
