using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record FollowLeagueCommand(int LeagueId, bool Followed) : ICommand<League>;

public class FollowLeagueHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<FollowLeagueCommand, League>
{
    public async ValueTask<League> Handle(FollowLeagueCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);
        var newlyFollowed = command.Followed && !league.IsFollowed;

        league.Follow(command.Followed);

        if (command.Followed && league.SupportsTeams && league.TeamsRefreshedAtUtc is null)
            await mediator.Send(new RefreshLeagueTeamsCommand(league.Id), ct);

        if (newlyFollowed)
        {
            foreach (var team in await workspace.Load(new TeamsByLeagueSpec(league.Id), ct))
                team.Follow(true);
        }

        if (command.Followed)
            await mediator.Send(new RefreshLeagueLogosCommand(league.Id), ct);

        return league;
    }
}
