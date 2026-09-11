using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sources;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record RefreshLeagueTeamsCommand(int LeagueId) : ICommand<int>;

public class RefreshLeagueTeamsHandler(
    IWorkspace workspace,
    IUnitOfWork unitOfWork,
    ISportsCatalogSource source,
    ILogger<RefreshLeagueTeamsHandler> logger)
    : ICommandHandler<RefreshLeagueTeamsCommand, int>
{
    public async ValueTask<int> Handle(RefreshLeagueTeamsCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (!league.SupportsTeams)
        {
            logger.LogInformation("League {League} has no teams to refresh.", league.Slug);
            return 0;
        }

        var sport = await workspace.LoadById<Sport, int>(league.SportId, ct);
        var key = new LeagueKey(sport.Slug, league.Slug, league.ExternalIdFor(source.SourceKey));

        var descriptors = await source.GetTeamsAsync(key, ct);
        var existing = (await workspace.Load(new TeamsByLeagueSpec(league.Id), ct))
            .ToDictionary(t => t.ExternalIdFor(source.SourceKey) ?? $"name:{t.DisplayName}");

        foreach (var descriptor in descriptors)
        {
            if (!existing.TryGetValue(descriptor.ExternalId, out var team)
                && !existing.TryGetValue($"name:{descriptor.DisplayName}", out team))
            {
                team = new Team(league.Id, descriptor.DisplayName, isFollowed: league.IsFollowed);
                workspace.Add(team);
                existing[descriptor.ExternalId] = team;
            }

            team.Describe(
                descriptor.DisplayName, descriptor.ShortDisplayName, descriptor.Slug, descriptor.Abbreviation,
                descriptor.Location, descriptor.Nickname, descriptor.PrimaryColorHex, descriptor.AlternateColorHex,
                descriptor.IsActive);
            team.TrackSource(source.SourceKey, descriptor.ExternalId);
        }

        league.MarkTeamsRefreshed();

        await unitOfWork.SaveChanges(ct);

        logger.LogInformation("Refreshed {Count} team(s) for {League}.", descriptors.Count, league.Slug);
        return descriptors.Count;
    }
}
