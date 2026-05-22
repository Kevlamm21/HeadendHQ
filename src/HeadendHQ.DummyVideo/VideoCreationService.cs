using System.Diagnostics;
using System.Text;
using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeadendHQ.DummyVideo;

public class VideoCreationService(
    IOptions<DummyVideoOptions> options,
    AppDbContext db,
    ILogger<VideoCreationService> logger) : IVideoCreationService
{
    private static readonly string TemplatePath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "3hr_template.mp4");

    public async Task CreateDailyAsync(CancellationToken ct = default)
    {
        var todayLocalStart = DateTime.Now.Date;
        var todayUtcStart = TimeZoneInfo.ConvertTimeToUtc(todayLocalStart, TimeZoneInfo.Local);
        var todayUtcEnd = TimeZoneInfo.ConvertTimeToUtc(todayLocalStart.AddDays(1), TimeZoneInfo.Local);

        var pending = await db.SportingEvents
            .Where(e => e.DummyVideoPath == null && e.StartUtc >= todayUtcStart && e.StartUtc < todayUtcEnd)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            logger.LogInformation("Video creation: no pending events for today.");
            return;
        }

        if (!File.Exists(TemplatePath))
        {
            logger.LogError("Template video not found at {Path}. Skipping video creation.", TemplatePath);
            return;
        }

        var created = 0;
        var failed = 0;

        foreach (var evt in pending)
        {
            try
            {
                evt.DummyVideoPath = await CreateVideoAsync(evt, ct);
                created++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to create video for event {Id} ({Title}).", evt.Id, evt.Title);
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Video creation complete. Created: {Created}, Failed: {Failed}.", created, failed);
    }

    private async Task<string> CreateVideoAsync(SportingEvent evt, CancellationToken ct)
    {
        var eventFolder = Path.Combine(options.Value.LibraryPath, evt.Title);
        Directory.CreateDirectory(eventFolder);

        var videoPath = Path.Combine(eventFolder, $"{evt.Title}.mp4");

        if (File.Exists(videoPath))
            return videoPath;

        var duration = evt.EndUtc - evt.StartUtc;

        if (duration.TotalHours >= 3)
        {
            File.Copy(TemplatePath, videoPath);
            return videoPath;
        }

        var durationSeconds = (int)Math.Ceiling(duration.TotalSeconds);
        var arguments = $"-i \"{TemplatePath}\" -t {durationSeconds} -c copy \"{videoPath}\"";
        await ExecuteFfmpegAsync(arguments, ct);

        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"FFmpeg completed but video not found at: {videoPath}");

        return videoPath;
    }

    private async Task ExecuteFfmpegAsync(string arguments, CancellationToken ct)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = options.Value.FfmpegPath,
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
