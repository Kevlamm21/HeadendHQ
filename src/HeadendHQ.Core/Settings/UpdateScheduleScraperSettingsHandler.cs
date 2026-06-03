using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Settings;

public record UpdateScheduleScraperSettingsCommand(
    List<StreamingService> EnabledStreamingServices,
    int ScrapeWindowDays,
    int CleanupRetentionDays,
    string CronSchedule,
    bool RunOnStartup) : ICommand<ScheduleScraperSettings>;

public class UpdateScheduleScraperSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateScheduleScraperSettingsCommand, ScheduleScraperSettings>
{
    public async ValueTask<ScheduleScraperSettings> Handle(UpdateScheduleScraperSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new ScheduleScraperSettingsSpec(), ct)
            ?? throw new InvalidOperationException("ScheduleScraperSettings not found.");

        settings.Configure(
            command.EnabledStreamingServices,
            command.ScrapeWindowDays,
            command.CleanupRetentionDays,
            command.CronSchedule,
            command.RunOnStartup);

        return settings;
    }
}
