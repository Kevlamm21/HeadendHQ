using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Teams.CommandHandlers;

public record UploadTeamLogoCommand(int TeamId, byte[] Bytes) : ICommand<TeamLogo>;

public class UploadTeamLogoHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadTeamLogoCommand, TeamLogo>
{
    public async ValueTask<TeamLogo> Handle(UploadTeamLogoCommand command, CancellationToken ct)
    {
        var team = await workspace.LoadById<Team, int>(command.TeamId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.TeamLogo), ct);

        return team.AddUploadedLogo(imageId);
    }
}
