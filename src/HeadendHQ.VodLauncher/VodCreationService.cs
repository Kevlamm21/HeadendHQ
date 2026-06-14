using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.VodLauncher.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.VodLauncher;

public class VodCreationService(
    IMediator mediator,
    IWorkspace workspace,
    IVideoCreator videoCreator,
    IImageCreationService imageCreation,
    INfoWriter nfoWriter,
    ILogger<VodCreationService> logger) : ICreationService
{
    private static readonly string TemplatePath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "3hr_template.mp4");

    public async Task CreateForTitleAsync(Guid titleId, CancellationToken ct = default)
    {
        var title = await workspace.LoadById<Title, Guid>(titleId, ct);

        var shouldCreateNow = title.StartUtc is null || title.StartUtc.Value.ToLocalTime().Date <= DateTime.Now.Date;
        if (!shouldCreateNow)
        {
            logger.LogInformation("Title {Id} ({Name}) is a future event, skipping production.", title.Id, title.Name);
            return;
        }

        if (title.VodLauncherPath is null)
        {
            var settings = await mediator.Send(new GetVodLauncherSettingsQuery(), ct);
            if (!settings.LibraryPaths.TryGetValue(title.Type, out var libraryPath))
            {
                logger.LogWarning("No library path for {Type}. Skipping VOD for {Id}.", title.Type, title.Id);
                return;
            }

            if (!File.Exists(TemplatePath))
            {
                logger.LogError("Template not found at {Path}. Skipping VOD for {Id}.", TemplatePath, title.Id);
                return;
            }

            title.VodLauncherPath = await CreateVideoAsync(title, libraryPath, ct);
        }

        if (!title.ArtworkCreated && title.Type == TitleType.SportingEvent)
        {
            try
            {
                await imageCreation.CreatePosterAsync(title, ct);
                await imageCreation.CreateThumbnailAsync(title, ct);
                await imageCreation.CreateBackgroundAsync(title, ct);
                await imageCreation.CreateClearLogoAsync(title, ct);
                title.ArtworkCreated = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to create artwork for title {Id} ({Name}).", title.Id, title.Name);
            }
        }

        try
        {
            await nfoWriter.WriteAsync(title, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to write NFO for title {Id} ({Name}).", title.Id, title.Name);
        }
    }

    private async Task<string> CreateVideoAsync(Title title, string libraryPath, CancellationToken ct)
    {
        var folder = Path.Combine(libraryPath, title.Name);
        Directory.CreateDirectory(folder);

        var videoPath = Path.Combine(folder, $"{title.Name}.mp4");

        if (File.Exists(videoPath))
            return folder;

        if (title.StartUtc is null)
        {
            File.Copy(TemplatePath, videoPath);
            return folder;
        }

        var startUtc = title.StartUtc.Value;
        var endUtc = title.EndUtc ?? startUtc.AddHours(3);
        var duration = endUtc - startUtc;

        if (duration.TotalHours >= 3)
        {
            File.Copy(TemplatePath, videoPath);
            return folder;
        }

        var durationSeconds = (int)Math.Ceiling(duration.TotalSeconds);
        await videoCreator.TrimVideoAsync(TemplatePath, videoPath, durationSeconds, ct);

        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"FFmpeg completed but video not found at: {videoPath}");

        return folder;
    }
}
