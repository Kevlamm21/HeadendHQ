using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Events;
using HeadendHQ.Core.Shared;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Core.Titles.CommandHandlers;

public record ComposeTitleArtworkCommand(Guid TitleId) : ICommand<Unit>;

public class ComposeTitleArtworkHandler(
    IWorkspace workspace,
    IImageCreationService imageCreation,
    ILogger<ComposeTitleArtworkHandler> logger)
    : ICommandHandler<ComposeTitleArtworkCommand, Unit>
{
    public async ValueTask<Unit> Handle(ComposeTitleArtworkCommand command, CancellationToken ct)
    {
        var title = await workspace.LoadById<Title, Guid>(command.TitleId, ct);

        if (title.ArtworkCreated)
            return Unit.Value;

        if (!title.Production.ComposesArtwork || title.SourceId is not { } sourceId)
            return Unit.Value;

        try
        {
            var posterId = await imageCreation.CreatePosterAsync(sourceId, ct);
            var thumbId = await imageCreation.CreateThumbAsync(sourceId, ct);
            var backdropId = await imageCreation.CreateBackdropAsync(sourceId, ct);
            var clearLogoId = await ResolveClearLogoAsync(sourceId, ct) ?? title.ClearLogoImageId;

            title.SetRenderedArtwork(posterId, backdropId, thumbId, clearLogoId);

            logger.LogInformation("Composed artwork for title {Id} ({Name}).", title.Id, title.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to compose artwork for title {Id} ({Name}).", title.Id, title.Name);
        }

        return Unit.Value;
    }

    private async Task<int?> ResolveClearLogoAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await workspace.LoadSingleOrDefault(new EntityByIdSpecification<SportingEvent, Guid>(eventId), ct);
        if (ev is null)
            return null;

        var league = await workspace.LoadById<League, int>(ev.LeagueId, ct);
        return league.WordmarkFor(ev.Variant)?.ImageId;
    }
}
