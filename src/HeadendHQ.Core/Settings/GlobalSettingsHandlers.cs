using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;

namespace HeadendHQ.Core.Settings;

public class GlobalSettingsSpec : ISpecification<GlobalSettings>
{
    public IQueryable<GlobalSettings> Apply(IQueryable<GlobalSettings> q) => q;
}

public record GetGlobalSettingsQuery : IQuery<GlobalSettings>;

public class GetGlobalSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetGlobalSettingsQuery, GlobalSettings>
{
    public async ValueTask<GlobalSettings> Handle(GetGlobalSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new GlobalSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("GlobalSettings not found.");
    }
}

public record UpdateGlobalSettingsCommand(
    List<StreamingService>? EnabledStreamingServices,
    int? TitleRetentionDays) : ICommand<GlobalSettings>;

public class UpdateGlobalSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateGlobalSettingsCommand, GlobalSettings>
{
    public async ValueTask<GlobalSettings> Handle(UpdateGlobalSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new GlobalSettingsSpec(), ct)
            ?? throw new InvalidOperationException("GlobalSettings not found.");

        settings.Configure(command.EnabledStreamingServices, command.TitleRetentionDays);
        return settings;
    }
}
