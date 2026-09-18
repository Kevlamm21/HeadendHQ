using HeadendHQ.Core.Media;
using HeadendHQ.Core.Media.CommandHandlers;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.VodLauncher.EventHandlers;

public record UploadTitleImagesCommand(
    Guid TitleId,
    Stream? Poster,
    Stream? Background,
    Stream? Thumbnail,
    Stream? Wordmark) : ICommand;

public class UploadTitleImagesHandler(
    IWorkspace workspace,
    IMediator mediator,
    ILogger<UploadTitleImagesHandler> logger) : ICommandHandler<UploadTitleImagesCommand>
{
    public async ValueTask<Unit> Handle(UploadTitleImagesCommand command, CancellationToken ct)
    {
        var title = await workspace.LoadById<Title, Guid>(command.TitleId, ct);

        var posterId = await StoreAsync(command.Poster, ImagePurpose.Poster, ct) ?? title.PosterImageId;
        var backgroundId = await StoreAsync(command.Background, ImagePurpose.Background, ct) ?? title.BackgroundImageId;
        var thumbnailId = await StoreAsync(command.Thumbnail, ImagePurpose.Thumbnail, ct) ?? title.ThumbnailImageId;
        var clearLogoId = await StoreAsync(command.Wordmark, ImagePurpose.Wordmark, ct) ?? title.ClearLogoImageId;

        if (posterId == title.PosterImageId
            && backgroundId == title.BackgroundImageId
            && thumbnailId == title.ThumbnailImageId
            && clearLogoId == title.ClearLogoImageId)
            return Unit.Value;

        var superseded = new[]
        {
            Superseded(title.PosterImageId, posterId),
            Superseded(title.BackgroundImageId, backgroundId),
            Superseded(title.ThumbnailImageId, thumbnailId),
            Superseded(title.ClearLogoImageId, clearLogoId),
        }.Where(id => id is not null).Select(id => id!.Value).ToArray();

        title.SetRenderedArtwork(posterId, backgroundId, thumbnailId, clearLogoId);

        if (superseded.Length > 0)
            await mediator.Send(new DeleteImagesCommand(superseded), ct);

        logger.LogInformation("Uploaded images for title {Id} ({Name}).", title.Id, title.Name);
        return Unit.Value;
    }

    private async Task<int?> StoreAsync(Stream? stream, ImagePurpose purpose, CancellationToken ct)
    {
        if (stream is null)
            return null;

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);

        return await mediator.Send(new UploadImageCommand(ms.ToArray(), purpose), ct);
    }

    private static int? Superseded(int? oldId, int? newId) =>
        oldId is { } id && id != newId ? id : null;
}
