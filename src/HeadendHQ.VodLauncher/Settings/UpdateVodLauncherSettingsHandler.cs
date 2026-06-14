using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.VodLauncher.Settings;

public record UpdateVodLauncherSettingsCommand(
    Dictionary<TitleType, string>? LibraryPaths) : ICommand<VodLauncherSettings>;

public class UpdateVodLauncherSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateVodLauncherSettingsCommand, VodLauncherSettings>
{
    public async ValueTask<VodLauncherSettings> Handle(UpdateVodLauncherSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new VodLauncherSettingsSpec(), ct)
            ?? throw new InvalidOperationException("VodLauncherSettings not found.");

        settings.Configure(command.LibraryPaths);

        return settings;
    }
}
