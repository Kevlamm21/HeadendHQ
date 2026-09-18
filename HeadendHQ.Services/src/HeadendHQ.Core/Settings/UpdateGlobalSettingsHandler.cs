using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Settings;

public record UpdateGlobalSettingsCommand(
    int? TitleRetentionDays,
    string? PublicBaseUrl) : ICommand<GlobalSettings>;

public class UpdateGlobalSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateGlobalSettingsCommand, GlobalSettings>
{
    public async ValueTask<GlobalSettings> Handle(UpdateGlobalSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new GlobalSettingsSpec(), ct)
            ?? throw new InvalidOperationException("GlobalSettings not found.");

        settings.Configure(command.TitleRetentionDays, command.PublicBaseUrl);
        return settings;
    }
}
