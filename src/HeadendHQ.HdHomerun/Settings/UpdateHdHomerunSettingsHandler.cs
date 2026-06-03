using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.HdHomerun.Settings;

public record UpdateHdHomerunSettingsCommand(
    string DeviceUrl,
    string CronSchedule,
    bool RunOnStartup) : ICommand<HdHomerunSettings>;

public class UpdateHdHomerunSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateHdHomerunSettingsCommand, HdHomerunSettings>
{
    public async ValueTask<HdHomerunSettings> Handle(UpdateHdHomerunSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new HdHomerunSettingsSpec(), ct)
            ?? throw new InvalidOperationException("HdHomerunSettings not found.");

        settings.Configure(command.DeviceUrl, command.CronSchedule, command.RunOnStartup);

        return settings;
    }
}
