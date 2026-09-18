using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

public record UploadLeagueLogoCommand(int LeagueId, string Variant, byte[] Bytes) : ICommand<LeagueLogo>;

public class UploadLeagueLogoHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadLeagueLogoCommand, LeagueLogo>
{
    public async ValueTask<LeagueLogo> Handle(UploadLeagueLogoCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.LeagueLogo), ct);

        return league.AddUploadedLogo(command.Variant, imageId);
    }
}
