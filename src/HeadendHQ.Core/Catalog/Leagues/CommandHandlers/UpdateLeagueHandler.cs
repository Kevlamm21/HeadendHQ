using HeadendHQ.Core.Catalog.Teams.CommandHandlers;
using HeadendHQ.Core.Catalog.Teams.Specifications;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record UpdateLeagueCommand(int LeagueId, bool? Followed, int? SelectedLogoId = null) : ICommand<League>;

public class UpdateLeagueHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UpdateLeagueCommand, League>
{
    public async ValueTask<League> Handle(UpdateLeagueCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        if (command.SelectedLogoId is { } logoId)
            league.SelectLogo(logoId);

        if (command.Followed is not { } followed)
            return league;

        var newlyFollowed = followed && !league.IsFollowed;

        league.Follow(followed);

        if (followed && league.SupportsTeams && league.TeamsRefreshedAtUtc is null)
            await mediator.Send(new RefreshLeagueTeamsCommand(league.Id), ct);

        if (newlyFollowed)
        {
            foreach (var team in await workspace.Load(new TeamsByLeagueSpec(league.Id), ct))
                team.Follow(true);
        }

        if (followed)
            await mediator.Send(new RefreshLeagueLogosCommand(league.Id), ct);

        return league;
    }
}
