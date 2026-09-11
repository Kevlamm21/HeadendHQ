using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.CommandHandlers;
using HeadendHQ.VodLauncher.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.VodLauncher;

public class VodCreationService(
    IMediator mediator,
    IWorkspace workspace,
    IVideoCreator videoCreator,
    INfoWriter nfoWriter,
    ILogger<VodCreationService> logger) : ICreationService
{
    private static readonly string TemplatePath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "3hr_template.mp4");

    public async Task CreateForTitleAsync(Guid titleId, CancellationToken ct = default)
    {
        var title = await workspace.LoadSingleOrDefault(new EntityByIdSpecification<Title, Guid>(titleId), ct);

        if (title is null)
        {
            logger.LogWarning("Title {Id} no longer exists. Skipping VOD production.", titleId);
            return;
        }

        // TEMP: EventUrl gate disabled until deep link aggregation is working — re-enable then.
        // if (title.EventUrl is null)
        // {
        //     logger.LogInformation("Title {Id} ({Name}) has no EventUrl, skipping VOD production.", title.Id, title.Name);
        //     return;
        // }

        var shouldCreateNow = title.StartUtc is null || title.StartUtc.Value.ToLocalTime().Date <= DateTime.Now.Date;
        if (!shouldCreateNow)
        {
            logger.LogInformation("Title {Id} ({Name}) is a future event, skipping production.", title.Id, title.Name);
            return;
        }

        if (!title.IsVideoCreated)
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

            title.SetVodLauncherPath(await CreateVideoAsync(title, libraryPath, ct));
            title.MarkVideoCreated(true);
        }

        if (!title.ArtworkCreated)
            await mediator.Send(new ComposeTitleArtworkCommand(title.Id), ct);

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
