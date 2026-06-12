using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles.Specifications;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.SixLabors.EventHandlers;

public record CreateArtworkCommand : ICommand<Unit>;

public class CreateArtworkHandler(
    IImageCreationService imageCreationService,
    IWorkspace workspace,
    ILogger<CreateArtworkHandler> logger) : ICommandHandler<CreateArtworkCommand, Unit>
{
    public async ValueTask<Unit> Handle(CreateArtworkCommand command, CancellationToken ct)
    {
        var titles = await workspace.Load(new TitlesNeedingArtworkSpec(), ct);

        foreach (var title in titles)
        {
            try
            {
                var posterPath = await imageCreationService.CreatePosterAsync(title, ct);
                await imageCreationService.CreateThumbnailAsync(title, ct);
                await imageCreationService.CreateClearLogoAsync(title, ct);
                title.PosterPath = posterPath;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Artwork creation failed for title '{Name}'.", title.Name);
            }
        }

        return Unit.Value;
    }
}
