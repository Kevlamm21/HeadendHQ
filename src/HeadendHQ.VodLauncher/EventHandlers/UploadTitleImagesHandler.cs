using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.VodLauncher.Settings;
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
    IImageNormalizer normalizer,
    ILogger<UploadTitleImagesHandler> logger) : ICommandHandler<UploadTitleImagesCommand>
{
    public async ValueTask<Unit> Handle(UploadTitleImagesCommand command, CancellationToken ct)
    {
        var title = await workspace.LoadById<Title, Guid>(command.TitleId, ct);

        if (title.VodLauncherPath is null)
        {
            var settings = await mediator.Send(new GetVodLauncherSettingsQuery(), ct);
            if (!settings.LibraryPaths.TryGetValue(title.Type, out var libraryPath))
            {
                logger.LogWarning("No library path configured for type {Type}. Cannot save images for title {Id}.", title.Type, title.Id);
                return Unit.Value;
            }

            title.VodLauncherPath = Path.Combine(libraryPath, title.Name);
        }

        Directory.CreateDirectory(title.VodLauncherPath);

        var uploaded = false;

        if (command.Poster is not null)
        {
            var bytes = await ReadAllBytesAsync(command.Poster, ct);
            var normalized = await normalizer.NormalizePosterAsync(bytes, ct);
            await File.WriteAllBytesAsync(Path.Combine(title.VodLauncherPath, $"{title.Name}.jpg"), normalized, ct);
            uploaded = true;
        }

        if (command.Background is not null)
        {
            var bytes = await ReadAllBytesAsync(command.Background, ct);
            var normalized = await normalizer.NormalizeBackgroundAsync(bytes, ct);
            await File.WriteAllBytesAsync(Path.Combine(title.VodLauncherPath, $"{title.Name}-fanart-1.jpg"), normalized, ct);
            uploaded = true;
        }

        if (command.Thumbnail is not null)
        {
            var bytes = await ReadAllBytesAsync(command.Thumbnail, ct);
            var normalized = await normalizer.NormalizeBackgroundAsync(bytes, ct);
            await File.WriteAllBytesAsync(Path.Combine(title.VodLauncherPath, $"{title.Name}-thumb.jpg"), normalized, ct);
            uploaded = true;
        }

        if (command.Wordmark is not null)
        {
            var bytes = await ReadAllBytesAsync(command.Wordmark, ct);
            var normalized = await normalizer.NormalizeWordMarkAsync(bytes, ct);
            await File.WriteAllBytesAsync(Path.Combine(title.VodLauncherPath, $"{title.Name}-clearlogo.png"), normalized, ct);
            uploaded = true;
        }

        if (uploaded)
        {
            title.ArtworkCreated = true;
            logger.LogInformation("Uploaded images for title {Id} ({Name}).", title.Id, title.Name);
        }

        return Unit.Value;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
