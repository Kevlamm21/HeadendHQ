using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record UploadLeagueLogoOverrideCommand(int LeagueId, string Variant, byte[] Bytes) : ICommand<League>;

public class UploadLeagueLogoOverrideHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadLeagueLogoOverrideCommand, League>
{
    public async ValueTask<League> Handle(UploadLeagueLogoOverrideCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.LeagueLogo), ct);
        league.UpsertLogo(command.Variant, LogoRels.Default, imageId, ImageOrigin.Manual);

        return league;
    }
}
