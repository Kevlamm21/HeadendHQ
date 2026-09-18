using HeadendHQ.Core.Catalog.Leagues.Specifications;
using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record RefreshFollowedLeaguesCommand : ICommand<int>;

public class RefreshFollowedLeaguesHandler(
    IReadModel readModel,
    IMediator mediator,
    ILogger<RefreshFollowedLeaguesHandler> logger)
    : ICommandHandler<RefreshFollowedLeaguesCommand, int>
{
    public async ValueTask<int> Handle(RefreshFollowedLeaguesCommand command, CancellationToken ct)
    {
        var leagues = await readModel.Search(new FollowedLeaguesSpec(), ct);
        var refreshed = 0;

        foreach (var league in leagues.Where(l => l.SupportsTeams))
        {
            try
            {
                await mediator.Send(new RefreshLeagueTeamsCommand(league.Id), ct);
                refreshed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to refresh teams for {League}.", league.Slug);
            }
        }

        return refreshed;
    }
}
