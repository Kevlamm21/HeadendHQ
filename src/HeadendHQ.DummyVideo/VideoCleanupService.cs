using HeadendHQ.Core;
using HeadendHQ.Core.Titles.Specifications;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.DummyVideo;

public class VideoCleanupService(
    IWorkspace workspace,
    IUnitOfWork uow,
    ILogger<VideoCleanupService> logger) : ICleanupService
{
    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        var expired = await workspace.Load(new ExpiredVideoTitlesSpec(DateTime.Now), ct);

        if (expired.Count == 0)
        {
            logger.LogInformation("Video cleanup: no expired videos to remove.");
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var title in expired)
        {
            try
            {
                var folder = Path.GetDirectoryName(title.DummyVideoPath!);
                if (folder != null && Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);

                title.DummyVideoPath = null;
                deleted++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to clean up video for title {Id} ({Name}).", title.Id, title.Name);
                failed++;
            }
        }

        await uow.SaveChanges(ct);
        logger.LogInformation("Video cleanup complete. Deleted: {Deleted}, Failed: {Failed}.", deleted, failed);
    }
}
