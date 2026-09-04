using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record UploadTeamLogoOverrideCommand(int TeamId, string Rel, byte[] Bytes) : ICommand<Team>;

public class UploadTeamLogoOverrideHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadTeamLogoOverrideCommand, Team>
{
    public async ValueTask<Team> Handle(UploadTeamLogoOverrideCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.TeamLogo), ct);
        team.UpsertLogo(command.Rel, imageId, ImageOrigin.Manual);

        // Pointless to upload a mark the team will not use, and the rel the user uploaded under is
        // the clearest statement of which one they meant.
        team.PreferLogo(command.Rel);

        return team;
    }
}
