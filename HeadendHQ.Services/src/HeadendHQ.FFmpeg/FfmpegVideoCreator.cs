using System.Diagnostics;
using System.Text;
using HeadendHQ.Core;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.FFmpeg;

public class FfmpegVideoCreator(ILogger<FfmpegVideoCreator> logger) : IVideoCreator
{
    public async Task TrimVideoAsync(string inputPath, string outputPath, int durationSeconds, CancellationToken ct = default)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{inputPath}\" -t {durationSeconds} -c copy \"{outputPath}\"",
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

        logger.LogInformation("FFmpeg trimmed {Input} to {Duration}s at {Output}.", inputPath, durationSeconds, outputPath);
    }
}
