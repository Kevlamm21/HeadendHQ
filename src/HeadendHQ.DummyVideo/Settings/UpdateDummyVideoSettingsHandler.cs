using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.DummyVideo.Settings;

public record UpdateDummyVideoSettingsCommand(
    Dictionary<TitleType, string> LibraryPaths,
    string CronSchedule,
    bool RunOnStartup) : ICommand<DummyVideoSettings>;

public class UpdateDummyVideoSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateDummyVideoSettingsCommand, DummyVideoSettings>
{
    public async ValueTask<DummyVideoSettings> Handle(UpdateDummyVideoSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new DummyVideoSettingsSpec(), ct)
            ?? throw new InvalidOperationException("DummyVideoSettings not found.");

        settings.Configure(command.LibraryPaths, command.CronSchedule, command.RunOnStartup);

        return settings;
    }
}
