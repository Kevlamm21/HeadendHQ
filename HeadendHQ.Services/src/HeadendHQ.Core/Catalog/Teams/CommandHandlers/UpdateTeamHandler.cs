using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record UpdateTeamCommand(
    int TeamId, bool? Followed, int? SelectedLogoId,
    string? PrimaryColorHex, string? AlternateColorHex) : ICommand<Team>;

public class UpdateTeamHandler(IWorkspace workspace)
    : ICommandHandler<UpdateTeamCommand, Team>
{
    public async ValueTask<Team> Handle(UpdateTeamCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        if (command.Followed is { } followed) team.Follow(followed);
        if (command.SelectedLogoId is { } logoId) team.SelectLogo(logoId);
        team.OverrideColors(command.PrimaryColorHex, command.AlternateColorHex);

        return team;
    }
}
