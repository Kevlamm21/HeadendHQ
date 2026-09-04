using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record UpdateTeamCommand(
    int TeamId, bool? Followed, string? PreferredLogoRel,
    string? PrimaryColorHex, string? AlternateColorHex) : ICommand<Team>;

public class UpdateTeamHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UpdateTeamCommand, Team>
{
    public async ValueTask<Team> Handle(UpdateTeamCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        if (command.Followed is { } followed) team.Follow(followed);
        if (command.PreferredLogoRel is { Length: > 0 } rel) team.PreferLogo(rel);
        team.OverrideColors(command.PrimaryColorHex, command.AlternateColorHex);

        // A followed team's artwork is needed soon; fetch it now rather than mid-scrape. The command
        // is a no-op when the chosen variant is already held, so this costs nothing on a plain
        // colour override or a re-follow.
        if (team.IsFollowed)
            await mediator.Send(new RefreshTeamLogosCommand(team.Id), ct);

        return team;
    }
}
