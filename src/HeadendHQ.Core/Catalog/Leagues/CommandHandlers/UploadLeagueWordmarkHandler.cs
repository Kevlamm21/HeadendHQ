using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using Mediator;

namespace HeadendHQ.Core.Catalog.Leagues.CommandHandlers;

/// <summary>
/// Hand uploads. Bytes are stored first and the row then points at them, so an upload is an ordinary
/// member of the collection rather than a special slot — and it is marked
/// <see cref="ImageOrigin.Manual"/>, which is what stops a later refresh from overwriting it.
/// </summary>
public record UploadLeagueWordmarkCommand(int LeagueId, string Variant, byte[] Bytes) : ICommand<League>;

public class UploadLeagueWordmarkHandler(IWorkspace workspace, IMediator mediator)
    : ICommandHandler<UploadLeagueWordmarkCommand, League>
{
    public async ValueTask<League> Handle(UploadLeagueWordmarkCommand command, CancellationToken ct)
    {
        var league = await workspace.LoadById<League, int>(command.LeagueId, ct);

        var imageId = await mediator.Send(new UploadImageCommand(command.Bytes, ImagePurpose.Wordmark), ct);
        league.UpsertWordmark(command.Variant, imageId);

        return league;
    }
}
