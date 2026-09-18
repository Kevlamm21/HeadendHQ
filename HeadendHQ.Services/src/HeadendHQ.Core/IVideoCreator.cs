namespace HeadendHQ.Core;

public interface IVideoCreator
{
    Task TrimVideoAsync(string inputPath, string outputPath, int durationSeconds, CancellationToken ct = default);
}
