using System.Diagnostics;
using System.Text;
using HeadendHQ.Core;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using HeadendHQ.Core.Titles.Specifications;
using HeadendHQ.DummyVideo.Settings;
using Mediator;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.DummyVideo;

public class VideoCreationService(
    IMediator mediator,
    IWorkspace workspace,
    ILogger<VideoCreationService> logger) : ICreationService
{
    private static readonly string TemplatePath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "3hr_template.mp4");

    public async Task CreateDailyAsync(CancellationToken ct = default)
    {
        var opts = await mediator.Send(new GetDummyVideoSettingsQuery(), ct);

        var todayLocalStart = DateTime.Now.Date;
        var todayUtcStart = TimeZoneInfo.ConvertTimeToUtc(todayLocalStart, TimeZoneInfo.Local);
        var todayUtcEnd = TimeZoneInfo.ConvertTimeToUtc(todayLocalStart.AddDays(1), TimeZoneInfo.Local);

        var pending = await workspace.Load(new TitlesNeedingVideoTodaySpec(todayUtcStart, todayUtcEnd), ct);

        if (pending.Count == 0)
        {
            logger.LogInformation("Video creation: no pending titles for today.");
            return;
        }

        if (!File.Exists(TemplatePath))
        {
            logger.LogError("Template video not found at {Path}. Skipping video creation.", TemplatePath);
            return;
        }

        var created = 0;
        var failed = 0;

        foreach (var title in pending)
        {
            if (!opts.LibraryPaths.TryGetValue(title.Type, out var libraryPath))
            {
                logger.LogWarning("No library path configured for title type {Type}. Skipping {Name}.", title.Type, title.Name);
                failed++;
                continue;
            }

            try
            {
                title.DummyVideoPath = await CreateVideoAsync(title, libraryPath, ct);
                created++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to create video for title {Id} ({Name}).", title.Id, title.Name);
                failed++;
            }
        }

        logger.LogInformation("Video creation complete. Created: {Created}, Failed: {Failed}.", created, failed);
    }

    private async Task<string> CreateVideoAsync(Title title, string libraryPath, CancellationToken ct)
    {
        var eventFolder = Path.Combine(libraryPath, title.Name);
        Directory.CreateDirectory(eventFolder);

        var videoPath = Path.Combine(eventFolder, $"{title.Name}.mp4");

        if (File.Exists(videoPath))
            return videoPath;

        var startUtc = title.StartUtc!.Value;
        var endUtc = title.EndUtc ?? startUtc.AddHours(3);
        var duration = endUtc - startUtc;

        if (duration.TotalHours >= 3)
        {
            File.Copy(TemplatePath, videoPath);
            return videoPath;
        }

        var durationSeconds = (int)Math.Ceiling(duration.TotalSeconds);
        await ExecuteFfmpegAsync($"-i \"{TemplatePath}\" -t {durationSeconds} -c copy \"{videoPath}\"", ct);

        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"FFmpeg completed but video not found at: {videoPath}");

        return videoPath;
    }

    private async Task ExecuteFfmpegAsync(string arguments, CancellationToken ct)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var errorBuilder = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"FFmpeg exited with code {process.ExitCode}. Error: {errorBuilder}");
    }
}
