using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Settings;

public class SourceSettingsSpec : ISpecification<SourceSettings>
{
    public IQueryable<SourceSettings> Apply(IQueryable<SourceSettings> q) => q;
}

public record GetSourceSettingsQuery : IQuery<SourceSettings>;

public class GetSourceSettingsHandler(IReadModel readModel)
    : IQueryHandler<GetSourceSettingsQuery, SourceSettings>
{
    public async ValueTask<SourceSettings> Handle(GetSourceSettingsQuery query, CancellationToken ct)
    {
        var settings = await readModel.SingleOrDefault(new SourceSettingsSpec(), ct);
        return settings ?? throw new InvalidOperationException("SourceSettings not found.");
    }
}

public record UpdateSourceSettingsCommand(
    int? RequestsPerMinute,
    int? MinDelayMs,
    int? JitterMs,
    int? MaxConcurrency,
    int? PerRunRequestBudget,
    string? UserAgent,
    int? RosterTtlDays,
    IReadOnlyList<string>? DiscoverySportSlugs = null,
    int? MaxTeamLogoLookupsPerRun = null) : ICommand<SourceSettings>;

public class UpdateSourceSettingsHandler(IWorkspace workspace)
    : ICommandHandler<UpdateSourceSettingsCommand, SourceSettings>
{
    public async ValueTask<SourceSettings> Handle(UpdateSourceSettingsCommand command, CancellationToken ct)
    {
        var settings = await workspace.LoadSingleOrDefault(new SourceSettingsSpec(), ct)
            ?? throw new InvalidOperationException("SourceSettings not found.");

        settings.Configure(
            command.RequestsPerMinute, command.MinDelayMs, command.JitterMs, command.MaxConcurrency,
            command.PerRunRequestBudget, command.UserAgent, command.RosterTtlDays,
            command.DiscoverySportSlugs, command.MaxTeamLogoLookupsPerRun);
        return settings;
    }
}
