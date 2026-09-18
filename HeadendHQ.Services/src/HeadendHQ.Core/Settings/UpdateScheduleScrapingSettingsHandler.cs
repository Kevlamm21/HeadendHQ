using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Settings;

public record UpdateScheduleScrapingSettingsCommand(int? ScrapeWindowDays, int? MaxAthletesPerTeam)
    : ICommand<ScheduleScrapingSettings>;

public class UpdateScheduleScrapingSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateScheduleScrapingSettingsCommand, ScheduleScrapingSettings>
{
    public async ValueTask<ScheduleScrapingSettings> Handle(UpdateScheduleScrapingSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new ScheduleScrapingSettingsSpec(), ct)
            ?? throw new InvalidOperationException("ScheduleScrapingSettings not found.");

        settings.Configure(command.ScrapeWindowDays, command.MaxAthletesPerTeam);
        return settings;
    }
}
