using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

/// <summary>
/// Following a league is the moment its teams become worth fetching, so the follow does that work
/// rather than leaving the user with an empty team picker.
/// </summary>
public record FollowLeagueCommand(int LeagueId, bool Followed) : ICommand<League>;

public class FollowLeagueHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<FollowLeagueCommand, League>
{
    public async ValueTask<League> Handle(FollowLeagueCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);
        league.Follow(command.Followed);

        if (command.Followed && league.SupportsTeams && league.TeamsRefreshedAtUtc is null)
            await mediator.Send(new RefreshLeagueTeamsCommand(league.Id), ct);

        // The league catalog records no artwork, so this is the first moment anything has wanted it.
        if (command.Followed)
            await mediator.Send(new RefreshLeagueLogosCommand(league.Id), ct);

        return league;
    }
}
