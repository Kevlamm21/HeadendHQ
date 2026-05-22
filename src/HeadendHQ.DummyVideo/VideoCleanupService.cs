using HeadendHQ.Core.SportingEvents;
using HeadendHQ.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.DummyVideo;

public class VideoCleanupService(
    AppDbContext db,
    ILogger<VideoCleanupService> logger) : IVideoCleanupService
{
    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;

        var expired = await db.SportingEvents
            .Where(e => e.DummyVideoPath != null && e.EndUtc < now)
            .ToListAsync(ct);

        if (expired.Count == 0)
        {
            logger.LogInformation("Video cleanup: no expired videos to remove.");
            return;
        }

        var deleted = 0;
        var failed = 0;

        foreach (var evt in expired)
        {
            try
            {
                var folder = Path.GetDirectoryName(evt.DummyVideoPath!);
                if (folder != null && Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);

                evt.DummyVideoPath = null;
                deleted++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to clean up video for event {Id} ({Title}).", evt.Id, evt.Title);
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Video cleanup complete. Deleted: {Deleted}, Failed: {Failed}.", deleted, failed);
    }
}
